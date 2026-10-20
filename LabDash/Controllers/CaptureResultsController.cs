using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class CaptureResultsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly ILogger<CaptureResultsController> _logger;

        public CaptureResultsController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            NotificationService notificationService,
            ILogger<CaptureResultsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var tests = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .Where(t =>
                    t.AssignedTechnicianId == technician.Id &&
                    (
                        t.Status == "In Progress" ||
                        t.Status == "To Be Reviewed"
                    ))
                .OrderBy(t => t.Status == "To Be Reviewed" ? 0 : 1)
                .ThenBy(t => t.StartDateTime)
                .ToListAsync();

            return View(tests);
        }

        // ============================================================
        // CAPTURE — GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Capture(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            if (item.Status != "In Progress" &&
                item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is no longer available for result capture.";

                return RedirectToAction(nameof(Index));
            }

            if (item.TestRequest == null)
            {
                TempData["Error"] =
                    "The test request could not be loaded.";

                return RedirectToAction(nameof(Index));
            }

            if (item.TestRequest.Patient == null)
            {
                TempData["Error"] =
                    "The patient information could not be loaded.";

                return RedirectToAction(nameof(Index));
            }

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            string? referenceRange = GetReferenceRange(item);
            string? automaticUnit = item.TestType?.UnitOfMeasurement;

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest.Patient;
            ViewBag.IsResubmission = item.Status == "To Be Reviewed";
            ViewBag.PreviousVerificationNote = existingResult?.VerificationNote;

            var model = new TestResult
            {
                TestRequestItemId = id,
                ResultValue = existingResult?.ResultValue,
                Units = automaticUnit,
                ReferenceRange = referenceRange ?? existingResult?.ReferenceRange,
                Comments = existingResult?.Comments
            };

            return View(model);
        }

        // ============================================================
        // CAPTURE — POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(TestResult result)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            if (result.TestRequestItemId <= 0)
            {
                TempData["Error"] = "Invalid test request item.";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == result.TestRequestItemId);

            if (item == null)
                return NotFound();

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            if (item.Status != "In Progress" &&
                item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is no longer available for result capture.";

                return RedirectToAction(nameof(Index));
            }

            if (item.TestRequest == null ||
                item.TestRequest.Patient == null)
            {
                TempData["Error"] =
                    "The patient or test request could not be loaded.";

                return RedirectToAction(nameof(Index));
            }

            string? automaticUnit = item.TestType?.UnitOfMeasurement;
            string? referenceRange = GetReferenceRange(item);

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(result.ResultValue))
            {
                ModelState.AddModelError(
                    nameof(result.ResultValue),
                    "Please enter a laboratory result.");
            }

            if (!ModelState.IsValid)
            {
                result.Units = automaticUnit;
                result.ReferenceRange = referenceRange;

                await PrepareCaptureView(item);

                return View(result);
            }

            bool isAbnormal = DetermineIfAbnormal(item, result.ResultValue);

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == result.TestRequestItemId);

            if (existingResult != null)
            {
                existingResult.ResultValue = result.ResultValue;
                existingResult.Units = automaticUnit;
                existingResult.ReferenceRange = referenceRange;
                existingResult.Comments = result.Comments;
                existingResult.IsAbnormal = isAbnormal;
                existingResult.DateCaptured = DateTime.Now;
                existingResult.CapturedByTechnicianId = technician.Id;

                existingResult.Status = "Completed";
                existingResult.VerifiedByTechnicianId = null;
                existingResult.VerificationDate = null;
                existingResult.VerificationNote = null;
            }
            else
            {
                result.DateCaptured = DateTime.Now;
                result.CapturedByTechnicianId = technician.Id;
                result.Status = "Completed";
                result.IsAbnormal = isAbnormal;
                result.Units = automaticUnit;
                result.ReferenceRange = referenceRange;
                result.VerifiedByTechnicianId = null;
                result.VerificationDate = null;
                result.VerificationNote = null;

                _context.TestResults.Add(result);
            }

            item.Status = "Completed";
            item.CompletionDateTime = DateTime.Now;

            var parentRequest = await _context.TestRequests
                .Include(r => r.TestRequestItems)
                .FirstOrDefaultAsync(r => r.RequestId == item.RequestId);

            bool parentJustCompleted = false;

            if (parentRequest != null && parentRequest.TestRequestItems != null)
            {
                bool allCompleted = parentRequest.TestRequestItems
                    .All(i =>
                        i.TestRequestItemId == item.TestRequestItemId
                            ? true
                            : i.Status == "Completed");

                bool canTransitionToCompleted =
                    parentRequest.Status != "Cancelled" &&
                    parentRequest.Status != "Released by doctor";

                if (allCompleted && canTransitionToCompleted)
                {
                    if (parentRequest.Status != "Completed")
                        parentJustCompleted = true;

                    parentRequest.Status = "Completed";
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "The laboratory result could not be saved: " +
                    (ex.InnerException?.Message ?? ex.Message));

                result.Units = automaticUnit;
                result.ReferenceRange = referenceRange;

                await PrepareCaptureView(item);

                return View(result);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred: " + ex.Message);

                result.Units = automaticUnit;
                result.ReferenceRange = referenceRange;

                await PrepareCaptureView(item);

                return View(result);
            }

            // =========================================================
            // NOTIFY DOCTOR — results ready (fires once per request)
            // =========================================================
            if (parentJustCompleted && parentRequest != null)
            {
                await NotifyDoctorResultsReadyAsync(parentRequest);
            }

            if (isAbnormal)
            {
                TempData["Success"] =
                    "Laboratory result saved successfully and flagged as abnormal.";
            }
            else
            {
                TempData["Success"] =
                    "Laboratory result saved successfully and sent for verification.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            var result = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .Include(r => r.VerifiedByTechnician)
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result has been captured for this test.";

                return RedirectToAction(nameof(Index));
            }

            if (item.TestType != null)
            {
                result.Units = item.TestType.UnitOfMeasurement;
                result.ReferenceRange = GetReferenceRange(item);
            }

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;

            return View(result);
        }

        // ============================================================
        // EDIT — GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            var result = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result exists to edit.";

                return RedirectToAction(nameof(Index));
            }

            if (result.Status != "Completed" &&
                result.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This laboratory result can no longer be edited.";

                return RedirectToAction(nameof(Index));
            }

            result.Units = item.TestType?.UnitOfMeasurement;
            result.ReferenceRange = GetReferenceRange(item);

            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.IsResubmission = item.Status == "To Be Reviewed";
            ViewBag.PreviousVerificationNote = result.VerificationNote;

            return View(result);
        }

        // ============================================================
        // EDIT — POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TestResult model)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            if (id <= 0)
            {
                TempData["Error"] = "Invalid laboratory result.";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(t => t.TestRequestItemId == id);

            if (item == null)
                return NotFound();

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == id);

            if (existingResult == null)
            {
                TempData["Error"] =
                    "The laboratory result could not be found.";

                return RedirectToAction(nameof(Index));
            }

            if (existingResult.Status != "Completed" &&
                existingResult.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This result can no longer be edited.";

                return RedirectToAction(nameof(Index));
            }

            string? automaticUnit = item.TestType?.UnitOfMeasurement;
            string? referenceRange = GetReferenceRange(item);

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(model.ResultValue))
            {
                ModelState.AddModelError(
                    nameof(model.ResultValue),
                    "Please enter a laboratory result.");
            }

            if (!ModelState.IsValid)
            {
                model.Units = automaticUnit;
                model.ReferenceRange = referenceRange;

                ViewBag.TestItem = item;
                ViewBag.Patient = item.TestRequest?.Patient;
                ViewBag.IsResubmission = item.Status == "To Be Reviewed";
                ViewBag.PreviousVerificationNote = existingResult.VerificationNote;

                return View(model);
            }

            bool isAbnormal = DetermineIfAbnormal(item, model.ResultValue);

            existingResult.ResultValue = model.ResultValue;
            existingResult.Units = automaticUnit;
            existingResult.ReferenceRange = referenceRange;
            existingResult.Comments = model.Comments;
            existingResult.IsAbnormal = isAbnormal;
            existingResult.DateCaptured = DateTime.Now;
            existingResult.CapturedByTechnicianId = technician.Id;

            existingResult.Status = "Completed";
            existingResult.VerifiedByTechnicianId = null;
            existingResult.VerificationDate = null;
            existingResult.VerificationNote = null;

            item.Status = "Completed";
            item.CompletionDateTime = DateTime.Now;

            var parentRequest = await _context.TestRequests
                .Include(r => r.TestRequestItems)
                .FirstOrDefaultAsync(r => r.RequestId == item.RequestId);

            bool parentJustCompleted = false;

            if (parentRequest != null && parentRequest.TestRequestItems != null)
            {
                bool allCompleted = parentRequest.TestRequestItems
                    .All(i =>
                        i.TestRequestItemId == item.TestRequestItemId
                            ? true
                            : i.Status == "Completed");

                bool canTransitionToCompleted =
                    parentRequest.Status != "Cancelled" &&
                    parentRequest.Status != "Released by doctor";

                if (allCompleted && canTransitionToCompleted)
                {
                    if (parentRequest.Status != "Completed")
                        parentJustCompleted = true;

                    parentRequest.Status = "Completed";
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "The laboratory result could not be updated: " +
                    (ex.InnerException?.Message ?? ex.Message));

                model.Units = automaticUnit;
                model.ReferenceRange = referenceRange;

                ViewBag.TestItem = item;
                ViewBag.Patient = item.TestRequest?.Patient;
                ViewBag.IsResubmission = item.Status == "To Be Reviewed";
                ViewBag.PreviousVerificationNote = existingResult.VerificationNote;

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred: " + ex.Message);

                model.Units = automaticUnit;
                model.ReferenceRange = referenceRange;

                ViewBag.TestItem = item;
                ViewBag.Patient = item.TestRequest?.Patient;

                return View(model);
            }

            // =========================================================
            // NOTIFY DOCTOR — results ready (fires once per request)
            // =========================================================
            if (parentJustCompleted && parentRequest != null)
            {
                await NotifyDoctorResultsReadyAsync(parentRequest);
            }

            if (isAbnormal)
            {
                TempData["Success"] =
                    "Laboratory result corrected successfully, flagged as abnormal, and sent for verification.";
            }
            else
            {
                TempData["Success"] =
                    "Laboratory result corrected successfully and sent for verification.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // PREPARE CAPTURE VIEW
        // ============================================================

        private async Task PrepareCaptureView(TestRequestItem item)
        {
            ViewBag.TestItem = item;
            ViewBag.Patient = item.TestRequest?.Patient;
            ViewBag.IsResubmission = item.Status == "To Be Reviewed";

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(r => r.TestRequestItemId == item.TestRequestItemId);

            ViewBag.PreviousVerificationNote = existingResult?.VerificationNote;
            ViewBag.ReferenceRange = GetReferenceRange(item);
            ViewBag.UnitOfMeasurement = item.TestType?.UnitOfMeasurement;
        }

        // ============================================================
        // GET REFERENCE RANGE
        // ============================================================

        private string? GetReferenceRange(TestRequestItem item)
        {
            if (item.TestType == null)
                return null;

            if (!item.TestType.ReferenceRangeLow.HasValue ||
                !item.TestType.ReferenceRangeHigh.HasValue)
            {
                return null;
            }

            return
                $"{item.TestType.ReferenceRangeLow.Value} - " +
                $"{item.TestType.ReferenceRangeHigh.Value}";
        }

        // ============================================================
        // DETERMINE IF RESULT IS ABNORMAL
        // ============================================================

        private bool DetermineIfAbnormal(TestRequestItem item, string? resultValue)
        {
            if (item.TestType == null)
                return false;

            if (!item.TestType.ReferenceRangeLow.HasValue ||
                !item.TestType.ReferenceRangeHigh.HasValue)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(resultValue))
                return false;

            if (!decimal.TryParse(resultValue, out decimal numericResult))
            {
                return false;
            }

            return
                numericResult < item.TestType.ReferenceRangeLow.Value ||
                numericResult > item.TestType.ReferenceRangeHigh.Value;
        }

        // ============================================================
        // NOTIFY REQUESTING DOCTOR — results ready
        // ============================================================

        private async Task NotifyDoctorResultsReadyAsync(TestRequest parentRequest)
        {
            try
            {
                string? doctorId = parentRequest.RequestingDoctorId;

                if (string.IsNullOrWhiteSpace(doctorId))
                {
                    _logger.LogInformation(
                        "No requesting doctor on request TR-{RequestId}. Notification skipped.",
                        parentRequest.RequestId);
                    return;
                }

                int requestId = parentRequest.RequestId;

                bool alreadySent = await _context.Notifications
                    .AsNoTracking()
                    .AnyAsync(n =>
                        n.RecipientUserId == doctorId &&
                        n.Type == "results-ready" &&
                        n.RelatedPatientID == parentRequest.PatientId &&
                        n.Message.Contains($"TR-{requestId}"));

                if (alreadySent)
                {
                    _logger.LogInformation(
                        "Doctor already notified for TR-{RequestId}. Skipping.",
                        requestId);
                    return;
                }

                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PatientID == parentRequest.PatientId);

                string patientName = patient == null
                    ? "the patient"
                    : $"{patient.Name} {patient.Surname}".Trim();

                string? linkUrl = Url.Action(
                    "Details",
                    "TestRequests",
                    new { id = requestId },
                    Request.Scheme);

                await _notificationService.SendToUserAsync(
                    recipientUserId: doctorId,
                    title: "Lab results ready for review",
                    message:
                        $"All results for request TR-{requestId} " +
                        $"({patientName}) have been captured and are ready for review.",
                    type: "results-ready",
                    linkUrl: linkUrl,
                    relatedPatientId: patient?.PatientID,
                    actorUserId: _userManager.GetUserId(User));

                _logger.LogInformation(
                    "Notified doctor {DoctorId} that TR-{RequestId} is ready.",
                    doctorId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to notify the requesting doctor for TR-{RequestId}.",
                    parentRequest.RequestId);
            }
        }
    }
}