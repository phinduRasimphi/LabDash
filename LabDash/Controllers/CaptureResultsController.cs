//using LabDash.Areas.Identity.Data;
//using LabDash.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace LabDash.Controllers
//{
//    [Authorize(Roles = "Lab_Technician")]
//    public class CaptureResultsController : Controller
//    {
//        private readonly LabDbContext _context;
//        private readonly UserManager<LabUser> _userManager;

//        public CaptureResultsController(
//            LabDbContext context,
//            UserManager<LabUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // ============================================================
//        // INDEX
//        // SHOW TESTS THAT NEED RESULTS
//        // ============================================================
//        [HttpGet]
//        public async Task<IActionResult> Index()
//        {
//            var technician = await _userManager.GetUserAsync(User);

//            if (technician == null)
//                return Challenge();

//            var tests = await _context.TestRequestItems
//                .Include(t => t.TestType)
//                .Include(t => t.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .Where(t =>
//                    t.AssignedTechnicianId == technician.Id &&
//                    (
//                        t.Status == "In Progress" ||
//                        t.Status == "To Be Reviewed"
//                    ))
//                .OrderBy(t => t.Status == "To Be Reviewed" ? 0 : 1)
//                .ThenBy(t => t.StartDateTime)
//                .ToListAsync();

//            return View(tests);
//        }


//        // ============================================================
//        // CAPTURE - GET
//        // OPEN NEW OR RETURNED RESULT
//        // ============================================================
//        [HttpGet]
//        public async Task<IActionResult> Capture(int id)
//        {
//            var technician = await _userManager.GetUserAsync(User);

//            if (technician == null)
//                return Challenge();

//            var item = await _context.TestRequestItems
//                .Include(t => t.TestType)
//                .Include(t => t.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .FirstOrDefaultAsync(
//                    t => t.TestRequestItemId == id
//                );

//            if (item == null)
//                return NotFound();

//            // Only assigned technician can capture
//            if (item.AssignedTechnicianId != technician.Id)
//                return Forbid();

//            // Only these statuses can be captured
//            if (item.Status != "In Progress" &&
//                item.Status != "To Be Reviewed")
//            {
//                TempData["Error"] =
//                    "This test is no longer available for result capture.";

//                return RedirectToAction(nameof(Index));
//            }

//            if (item.TestRequest == null)
//            {
//                TempData["Error"] =
//                    "The test request could not be loaded.";

//                return RedirectToAction(nameof(Index));
//            }

//            if (item.TestRequest.Patient == null)
//            {
//                TempData["Error"] =
//                    "The patient information could not be loaded.";

//                return RedirectToAction(nameof(Index));
//            }

//            var existingResult = await _context.TestResults
//                .FirstOrDefaultAsync(
//                    r => r.TestRequestItemId == id
//                );

//            ViewBag.TestItem = item;
//            ViewBag.Patient = item.TestRequest.Patient;

//            ViewBag.IsResubmission =
//                item.Status == "To Be Reviewed";

//            ViewBag.PreviousVerificationNote =
//                existingResult?.VerificationNote;

//            string? referenceRange = null;

//            if (item.TestType != null &&
//                item.TestType.ReferenceRangeLow.HasValue &&
//                item.TestType.ReferenceRangeHigh.HasValue)
//            {
//                referenceRange =
//                    $"{item.TestType.ReferenceRangeLow.Value} - " +
//                    $"{item.TestType.ReferenceRangeHigh.Value}";
//            }

//            var model = new TestResult
//            {
//                TestRequestItemId = id,

//                ResultValue =
//        existingResult?.ResultValue,

//                // Automatically get units from the Test Type
//                Units =
//        item.TestType?.UnitOfMeasurement,

//                ReferenceRange =
//        referenceRange ??
//        existingResult?.ReferenceRange,

//                Comments =
//        existingResult?.Comments
//            };

//            return View(model);
//        }


//        // ============================================================
//        // CAPTURE - POST
//        // SAVE LABORATORY RESULT
//        // ============================================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Capture(TestResult result)
//        {
//            var technician = await _userManager.GetUserAsync(User);

//            if (technician == null)
//                return Challenge();

//            if (result.TestRequestItemId <= 0)
//            {
//                TempData["Error"] =
//                    "Invalid test request item.";

//                return RedirectToAction(nameof(Index));
//            }

//            var item = await _context.TestRequestItems
//                .Include(t => t.TestType)
//                .Include(t => t.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .FirstOrDefaultAsync(
//                    t => t.TestRequestItemId ==
//                         result.TestRequestItemId
//                );

//            if (item == null)
//                return NotFound();

//            // Only assigned technician can capture
//            if (item.AssignedTechnicianId != technician.Id)
//                return Forbid();

//            // Only In Progress / To Be Reviewed
//            if (item.Status != "In Progress" &&
//                item.Status != "To Be Reviewed")
//            {
//                TempData["Error"] =
//                    "This test is no longer available for result capture.";

//                return RedirectToAction(nameof(Index));
//            }

//            if (item.TestRequest == null ||
//                item.TestRequest.Patient == null)
//            {
//                TempData["Error"] =
//                    "The patient or test request could not be loaded.";

//                return RedirectToAction(nameof(Index));
//            }

//            // --------------------------------------------------------
//            // MANUAL VALIDATION
//            // --------------------------------------------------------

//            ModelState.Clear();

//            if (string.IsNullOrWhiteSpace(result.ResultValue))
//            {
//                ModelState.AddModelError(
//                    nameof(result.ResultValue),
//                    "Please enter a laboratory result."
//                );
//            }

//            if (!ModelState.IsValid)
//            {
//                await PrepareCaptureView(item);

//                return View(result);
//            }

//            // --------------------------------------------------------
//            // DETERMINE ABNORMAL RESULT
//            // --------------------------------------------------------

//            bool isAbnormal = false;

//            if (item.TestType != null &&
//                item.TestType.ReferenceRangeLow.HasValue &&
//                item.TestType.ReferenceRangeHigh.HasValue)
//            {
//                if (decimal.TryParse(
//                    result.ResultValue,
//                    out decimal numericResult))
//                {
//                    isAbnormal =
//                        numericResult <
//                        item.TestType.ReferenceRangeLow.Value
//                        ||
//                        numericResult >
//                        item.TestType.ReferenceRangeHigh.Value;
//                }
//            }

//            // --------------------------------------------------------
//            // AUTOMATIC REFERENCE RANGE
//            // --------------------------------------------------------

//            string? referenceRange =
//                result.ReferenceRange;

//            if (item.TestType != null &&
//                item.TestType.ReferenceRangeLow.HasValue &&
//                item.TestType.ReferenceRangeHigh.HasValue)
//            {
//                referenceRange =
//                    $"{item.TestType.ReferenceRangeLow.Value} - " +
//                    $"{item.TestType.ReferenceRangeHigh.Value}";
//            }

//            // --------------------------------------------------------
//            // FIND EXISTING RESULT
//            // --------------------------------------------------------

//            var existingResult = await _context.TestResults
//                .FirstOrDefaultAsync(
//                    r => r.TestRequestItemId ==
//                         result.TestRequestItemId
//                );

//            // ========================================================
//            // UPDATE EXISTING RESULT
//            // ========================================================

//            if (existingResult != null)
//            {
//                existingResult.ResultValue =
//                    result.ResultValue;

//                existingResult.Units =
//                    result.Units;

//                existingResult.ReferenceRange =
//                    referenceRange;

//                existingResult.Comments =
//                    result.Comments;

//                existingResult.IsAbnormal =
//                    isAbnormal;

//                existingResult.DateCaptured =
//                    DateTime.Now;

//                existingResult.CapturedByTechnicianId =
//                    technician.Id;

//                // Send back through verification
//                existingResult.Status =
//                    "Completed";

//                // It has NOT yet been verified
//                existingResult.VerifiedByTechnicianId =
//                    null;

//                existingResult.VerificationDate =
//                    null;

//                // Clear current verification note.
//                // Previous review records remain in TestVerifications.
//                existingResult.VerificationNote =
//                    null;
//            }

//            // ========================================================
//            // CREATE NEW RESULT
//            // ========================================================

//            else
//            {
//                result.DateCaptured =
//                    DateTime.Now;

//                result.CapturedByTechnicianId =
//                    technician.Id;

//                result.Status =
//                    "Completed";

//                result.IsAbnormal =
//                    isAbnormal;

//                result.ReferenceRange =
//                    referenceRange;

//                result.VerifiedByTechnicianId =
//                    null;

//                result.VerificationDate =
//                    null;

//                _context.TestResults.Add(result);
//            }

//            // ========================================================
//            // UPDATE TEST ITEM
//            // ========================================================

//            item.Status =
//                "Completed";

//            item.CompletionDateTime =
//                DateTime.Now;

//            // ========================================================
//            // SAVE
//            // ========================================================

//            try
//            {
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateException ex)
//            {
//                ModelState.AddModelError(
//                    "",
//                    "The laboratory result could not be saved: " +
//                    (ex.InnerException?.Message ?? ex.Message)
//                );

//                await PrepareCaptureView(item);

//                return View(result);
//            }
//            catch (Exception ex)
//            {
//                ModelState.AddModelError(
//                    "",
//                    "An unexpected error occurred: " +
//                    ex.Message
//                );

//                await PrepareCaptureView(item);

//                return View(result);
//            }

//            // ========================================================
//            // SUCCESS
//            // ========================================================

//            if (isAbnormal)
//            {
//                TempData["Success"] =
//                    "Laboratory result saved successfully and flagged as abnormal.";
//            }
//            else
//            {
//                TempData["Success"] =
//                    "Laboratory result saved successfully and sent for verification.";
//            }

//            return RedirectToAction(nameof(Index));
//        }


//        // ============================================================
//        // DETAILS
//        // VIEW LABORATORY RESULT
//        // ============================================================
//        [HttpGet]
//        public async Task<IActionResult> Details(int id)
//        {
//            var technician = await _userManager.GetUserAsync(User);

//            if (technician == null)
//                return Challenge();

//            var item = await _context.TestRequestItems
//                .Include(t => t.TestType)
//                .Include(t => t.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .FirstOrDefaultAsync(
//                    t => t.TestRequestItemId == id
//                );

//            if (item == null)
//                return NotFound();

//            // Only assigned technician can view
//            if (item.AssignedTechnicianId != technician.Id)
//                return Forbid();

//            var result = await _context.TestResults
//                .Include(r => r.CapturedByTechnician)
//                .Include(r => r.VerifiedByTechnician)
//                .FirstOrDefaultAsync(
//                    r => r.TestRequestItemId == id
//                );

//            if (result == null)
//            {
//                TempData["Error"] =
//                    "No laboratory result has been captured for this test.";

//                return RedirectToAction(nameof(Index));
//            }

//            ViewBag.TestItem = item;
//            ViewBag.Patient = item.TestRequest?.Patient;

//            return View(result);
//        }


//        // ============================================================
//        // EDIT - GET
//        // EDIT / CORRECT RESULT
//        // ============================================================
//        [HttpGet]
//        public async Task<IActionResult> Edit(int id)
//        {
//            var technician = await _userManager.GetUserAsync(User);

//            if (technician == null)
//                return Challenge();

//            var item = await _context.TestRequestItems
//                .Include(t => t.TestType)
//                .Include(t => t.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .FirstOrDefaultAsync(
//                    t => t.TestRequestItemId == id
//                );

//            if (item == null)
//                return NotFound();

//            if (item.AssignedTechnicianId != technician.Id)
//                return Forbid();

//            var result = await _context.TestResults
//                .FirstOrDefaultAsync(
//                    r => r.TestRequestItemId == id
//                );

//            if (result == null)
//            {
//                TempData["Error"] =
//                    "No laboratory result exists to edit.";

//                return RedirectToAction(nameof(Index));
//            }

//            // Only these statuses may be edited
//            if (result.Status != "Completed" &&
//                result.Status != "To Be Reviewed")
//            {
//                TempData["Error"] =
//                    "This laboratory result can no longer be edited.";

//                return RedirectToAction(nameof(Index));
//            }

//            ViewBag.TestItem = item;
//            ViewBag.Patient = item.TestRequest?.Patient;

//            ViewBag.IsResubmission =
//                item.Status == "To Be Reviewed";

//            ViewBag.PreviousVerificationNote =
//                result.VerificationNote;

//            return View(result);
//        }


//        // ============================================================
//        // EDIT - POST
//        // SAVE CORRECTED RESULT
//        // ============================================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(
//            int id,
//            TestResult model)
//        {
//            var technician = await _userManager.GetUserAsync(User);

//            if (technician == null)
//                return Challenge();

//            if (id <= 0)
//            {
//                TempData["Error"] =
//                    "Invalid laboratory result.";

//                return RedirectToAction(nameof(Index));
//            }

//            var item = await _context.TestRequestItems
//                .Include(t => t.TestType)
//                .Include(t => t.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .FirstOrDefaultAsync(
//                    t => t.TestRequestItemId == id
//                );

//            if (item == null)
//                return NotFound();

//            if (item.AssignedTechnicianId != technician.Id)
//                return Forbid();

//            var existingResult = await _context.TestResults
//                .FirstOrDefaultAsync(
//                    r => r.TestRequestItemId == id
//                );

//            if (existingResult == null)
//            {
//                TempData["Error"] =
//                    "The laboratory result could not be found.";

//                return RedirectToAction(nameof(Index));
//            }

//            // --------------------------------------------------------
//            // ONLY COMPLETED OR RETURNED RESULTS CAN BE EDITED
//            // --------------------------------------------------------

//            if (existingResult.Status != "Completed" &&
//                existingResult.Status != "To Be Reviewed")
//            {
//                TempData["Error"] =
//                    "This result can no longer be edited.";

//                return RedirectToAction(nameof(Index));
//            }

//            // --------------------------------------------------------
//            // VALIDATE
//            // --------------------------------------------------------

//            ModelState.Clear();

//            if (string.IsNullOrWhiteSpace(model.ResultValue))
//            {
//                ModelState.AddModelError(
//                    nameof(model.ResultValue),
//                    "Please enter a laboratory result."
//                );
//            }

//            if (!ModelState.IsValid)
//            {
//                ViewBag.TestItem = item;
//                ViewBag.Patient = item.TestRequest?.Patient;

//                ViewBag.IsResubmission =
//                    item.Status == "To Be Reviewed";

//                ViewBag.PreviousVerificationNote =
//                    existingResult.VerificationNote;

//                return View(model);
//            }

//            // --------------------------------------------------------
//            // DETERMINE ABNORMAL
//            // --------------------------------------------------------

//            bool isAbnormal = false;

//            if (item.TestType != null &&
//                item.TestType.ReferenceRangeLow.HasValue &&
//                item.TestType.ReferenceRangeHigh.HasValue)
//            {
//                if (decimal.TryParse(
//                    model.ResultValue,
//                    out decimal numericResult))
//                {
//                    isAbnormal =
//                        numericResult <
//                        item.TestType.ReferenceRangeLow.Value
//                        ||
//                        numericResult >
//                        item.TestType.ReferenceRangeHigh.Value;
//                }
//            }

//            // --------------------------------------------------------
//            // REFERENCE RANGE
//            // --------------------------------------------------------

//            string? referenceRange =
//                model.ReferenceRange;

//            if (item.TestType != null &&
//                item.TestType.ReferenceRangeLow.HasValue &&
//                item.TestType.ReferenceRangeHigh.HasValue)
//            {
//                referenceRange =
//                    $"{item.TestType.ReferenceRangeLow.Value} - " +
//                    $"{item.TestType.ReferenceRangeHigh.Value}";
//            }

//            // ========================================================
//            // UPDATE EXISTING RESULT
//            // ========================================================

//            existingResult.ResultValue =
//                model.ResultValue;

//            existingResult.Units =
//                model.Units;

//            existingResult.ReferenceRange =
//                referenceRange;

//            existingResult.Comments =
//                model.Comments;

//            existingResult.IsAbnormal =
//                isAbnormal;

//            existingResult.DateCaptured =
//                DateTime.Now;

//            existingResult.CapturedByTechnicianId =
//                technician.Id;

//            // --------------------------------------------------------
//            // SEND BACK THROUGH VERIFICATION
//            // --------------------------------------------------------

//            existingResult.Status =
//                "Completed";

//            existingResult.VerifiedByTechnicianId =
//                null;

//            existingResult.VerificationDate =
//                null;

//            existingResult.VerificationNote =
//                null;

//            // ========================================================
//            // UPDATE TEST ITEM
//            // ========================================================

//            item.Status =
//                "Completed";

//            item.CompletionDateTime =
//                DateTime.Now;

//            // ========================================================
//            // SAVE
//            // ========================================================

//            try
//            {
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateException ex)
//            {
//                ModelState.AddModelError(
//                    "",
//                    "The laboratory result could not be updated: " +
//                    (ex.InnerException?.Message ?? ex.Message)
//                );

//                ViewBag.TestItem = item;
//                ViewBag.Patient = item.TestRequest?.Patient;

//                ViewBag.IsResubmission =
//                    false;

//                return View(model);
//            }
//            catch (Exception ex)
//            {
//                ModelState.AddModelError(
//                    "",
//                    "An unexpected error occurred: " +
//                    ex.Message
//                );

//                ViewBag.TestItem = item;
//                ViewBag.Patient = item.TestRequest?.Patient;

//                return View(model);
//            }

//            // ========================================================
//            // SUCCESS
//            // ========================================================

//            if (isAbnormal)
//            {
//                TempData["Success"] =
//                    "Laboratory result corrected successfully, flagged as abnormal, and sent for verification.";
//            }
//            else
//            {
//                TempData["Success"] =
//                    "Laboratory result corrected successfully and sent for verification.";
//            }

//            return RedirectToAction(nameof(Index));
//        }


//        // ============================================================
//        // PREPARE CAPTURE VIEW
//        // ============================================================
//        private async Task PrepareCaptureView(
//            TestRequestItem item)
//        {
//            ViewBag.TestItem =
//                item;

//            ViewBag.Patient =
//                item.TestRequest?.Patient;

//            ViewBag.IsResubmission =
//                item.Status == "To Be Reviewed";

//            var existingResult =
//                await _context.TestResults
//                    .FirstOrDefaultAsync(
//                        r => r.TestRequestItemId ==
//                             item.TestRequestItemId
//                    );

//            ViewBag.PreviousVerificationNote =
//                existingResult?.VerificationNote;

//            string? referenceRange = null;

//            if (item.TestType != null &&
//                item.TestType.ReferenceRangeLow.HasValue &&
//                item.TestType.ReferenceRangeHigh.HasValue)
//            {
//                referenceRange =
//                    $"{item.TestType.ReferenceRangeLow.Value} - " +
//                    $"{item.TestType.ReferenceRangeHigh.Value}";
//            }

//            ViewBag.ReferenceRange =
//                referenceRange;
//        }
//    }
//}
using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Lab_Technician")]
    public class CaptureResultsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public CaptureResultsController(
            LabDbContext context,
            UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // INDEX
        // SHOW TESTS THAT NEED RESULTS
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
        // CAPTURE - GET
        // OPEN NEW OR RETURNED RESULT
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
                .FirstOrDefaultAsync(
                    t => t.TestRequestItemId == id
                );

            if (item == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY ASSIGNED TECHNICIAN CAN CAPTURE
            // --------------------------------------------------------

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            // --------------------------------------------------------
            // CHECK STATUS
            // --------------------------------------------------------

            if (item.Status != "In Progress" &&
                item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is no longer available for result capture.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // CHECK TEST REQUEST
            // --------------------------------------------------------

            if (item.TestRequest == null)
            {
                TempData["Error"] =
                    "The test request could not be loaded.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // CHECK PATIENT
            // --------------------------------------------------------

            if (item.TestRequest.Patient == null)
            {
                TempData["Error"] =
                    "The patient information could not be loaded.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // FIND EXISTING RESULT
            // --------------------------------------------------------

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId == id
                );

            // --------------------------------------------------------
            // AUTOMATIC REFERENCE RANGE
            // --------------------------------------------------------

            string? referenceRange = GetReferenceRange(item);

            // --------------------------------------------------------
            // AUTOMATIC UNIT OF MEASUREMENT
            // IMPORTANT:
            // The unit comes from TestType.
            // The technician does NOT enter it.
            // --------------------------------------------------------

            string? automaticUnit =
                item.TestType?.UnitOfMeasurement;

            // --------------------------------------------------------
            // VIEW DATA
            // --------------------------------------------------------

            ViewBag.TestItem = item;

            ViewBag.Patient =
                item.TestRequest.Patient;

            ViewBag.IsResubmission =
                item.Status == "To Be Reviewed";

            ViewBag.PreviousVerificationNote =
                existingResult?.VerificationNote;

            // --------------------------------------------------------
            // CREATE MODEL FOR VIEW
            // --------------------------------------------------------

            var model = new TestResult
            {
                TestRequestItemId = id,

                ResultValue =
                    existingResult?.ResultValue,

                // ALWAYS GET UNIT FROM TEST TYPE
                Units =
                    automaticUnit,

                // ALWAYS PREFER TEST TYPE REFERENCE RANGE
                ReferenceRange =
                    referenceRange ??
                    existingResult?.ReferenceRange,

                Comments =
                    existingResult?.Comments
            };

            return View(model);
        }


        // ============================================================
        // CAPTURE - POST
        // SAVE LABORATORY RESULT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(TestResult result)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // --------------------------------------------------------
            // BASIC ID VALIDATION
            // --------------------------------------------------------

            if (result.TestRequestItemId <= 0)
            {
                TempData["Error"] =
                    "Invalid test request item.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // LOAD TEST ITEM
            // --------------------------------------------------------

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(
                    t => t.TestRequestItemId ==
                         result.TestRequestItemId
                );

            if (item == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY ASSIGNED TECHNICIAN CAN CAPTURE
            // --------------------------------------------------------

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            // --------------------------------------------------------
            // CHECK STATUS
            // --------------------------------------------------------

            if (item.Status != "In Progress" &&
                item.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This test is no longer available for result capture.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // CHECK TEST REQUEST AND PATIENT
            // --------------------------------------------------------

            if (item.TestRequest == null ||
                item.TestRequest.Patient == null)
            {
                TempData["Error"] =
                    "The patient or test request could not be loaded.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // AUTOMATIC UNIT
            // --------------------------------------------------------

            string? automaticUnit =
                item.TestType?.UnitOfMeasurement;

            // --------------------------------------------------------
            // AUTOMATIC REFERENCE RANGE
            // --------------------------------------------------------

            string? referenceRange =
                GetReferenceRange(item);

            // --------------------------------------------------------
            // MANUAL VALIDATION
            // --------------------------------------------------------

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(result.ResultValue))
            {
                ModelState.AddModelError(
                    nameof(result.ResultValue),
                    "Please enter a laboratory result."
                );
            }

            if (!ModelState.IsValid)
            {
                // Never trust posted Units.
                result.Units = automaticUnit;

                // Never trust posted ReferenceRange.
                result.ReferenceRange =
                    referenceRange;

                await PrepareCaptureView(item);

                return View(result);
            }

            // --------------------------------------------------------
            // DETERMINE ABNORMAL RESULT
            // --------------------------------------------------------

            bool isAbnormal =
                DetermineIfAbnormal(
                    item,
                    result.ResultValue
                );

            // --------------------------------------------------------
            // FIND EXISTING RESULT
            // --------------------------------------------------------

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId ==
                         result.TestRequestItemId
                );

            // ========================================================
            // UPDATE EXISTING RESULT
            // ========================================================

            if (existingResult != null)
            {
                existingResult.ResultValue =
                    result.ResultValue;

                // IMPORTANT:
                // Units ALWAYS COME FROM TestType
                existingResult.Units =
                    automaticUnit;

                // IMPORTANT:
                // Reference range ALWAYS COMES FROM TestType
                existingResult.ReferenceRange =
                    referenceRange;

                existingResult.Comments =
                    result.Comments;

                existingResult.IsAbnormal =
                    isAbnormal;

                existingResult.DateCaptured =
                    DateTime.Now;

                existingResult.CapturedByTechnicianId =
                    technician.Id;

                // Send back through verification
                existingResult.Status =
                    "Completed";

                // It has not yet been verified
                existingResult.VerifiedByTechnicianId =
                    null;

                existingResult.VerificationDate =
                    null;

                // Clear current verification note
                existingResult.VerificationNote =
                    null;
            }

            // ========================================================
            // CREATE NEW RESULT
            // ========================================================

            else
            {
                result.DateCaptured =
                    DateTime.Now;

                result.CapturedByTechnicianId =
                    technician.Id;

                result.Status =
                    "Completed";

                result.IsAbnormal =
                    isAbnormal;

                // IMPORTANT:
                // Automatically set from TestType
                result.Units =
                    automaticUnit;

                // Automatically set from TestType
                result.ReferenceRange =
                    referenceRange;

                result.VerifiedByTechnicianId =
                    null;

                result.VerificationDate =
                    null;

                result.VerificationNote =
                    null;

                _context.TestResults.Add(result);
            }

            // ========================================================
            // UPDATE TEST ITEM
            // ========================================================

            item.Status =
                "Completed";

            item.CompletionDateTime =
                DateTime.Now;

            // ========================================================
            // SAVE
            // ========================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "The laboratory result could not be saved: " +
                    (ex.InnerException?.Message ?? ex.Message)
                );

                result.Units =
                    automaticUnit;

                result.ReferenceRange =
                    referenceRange;

                await PrepareCaptureView(item);

                return View(result);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred: " +
                    ex.Message
                );

                result.Units =
                    automaticUnit;

                result.ReferenceRange =
                    referenceRange;

                await PrepareCaptureView(item);

                return View(result);
            }

            // ========================================================
            // SUCCESS MESSAGE
            // ========================================================

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
        // VIEW LABORATORY RESULT
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
                .FirstOrDefaultAsync(
                    t => t.TestRequestItemId == id
                );

            if (item == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY ASSIGNED TECHNICIAN CAN VIEW
            // --------------------------------------------------------

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            // --------------------------------------------------------
            // LOAD RESULT
            // --------------------------------------------------------

            var result = await _context.TestResults
                .Include(r => r.CapturedByTechnician)
                .Include(r => r.VerifiedByTechnician)
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId == id
                );

            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result has been captured for this test.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // ENSURE DISPLAYED UNIT MATCHES TEST TYPE
            // --------------------------------------------------------

            if (item.TestType != null)
            {
                result.Units =
                    item.TestType.UnitOfMeasurement;

                result.ReferenceRange =
                    GetReferenceRange(item);
            }

            ViewBag.TestItem =
                item;

            ViewBag.Patient =
                item.TestRequest?.Patient;

            return View(result);
        }


        // ============================================================
        // EDIT - GET
        // EDIT / CORRECT RESULT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // --------------------------------------------------------
            // LOAD TEST ITEM
            // --------------------------------------------------------

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(
                    t => t.TestRequestItemId == id
                );

            if (item == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY ASSIGNED TECHNICIAN
            // --------------------------------------------------------

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            // --------------------------------------------------------
            // LOAD RESULT
            // --------------------------------------------------------

            var result = await _context.TestResults
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId == id
                );

            if (result == null)
            {
                TempData["Error"] =
                    "No laboratory result exists to edit.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // CHECK RESULT STATUS
            // --------------------------------------------------------

            if (result.Status != "Completed" &&
                result.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This laboratory result can no longer be edited.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // AUTOMATIC VALUES
            // --------------------------------------------------------

            result.Units =
                item.TestType?.UnitOfMeasurement;

            result.ReferenceRange =
                GetReferenceRange(item);

            // --------------------------------------------------------
            // VIEW DATA
            // --------------------------------------------------------

            ViewBag.TestItem =
                item;

            ViewBag.Patient =
                item.TestRequest?.Patient;

            ViewBag.IsResubmission =
                item.Status == "To Be Reviewed";

            ViewBag.PreviousVerificationNote =
                result.VerificationNote;

            return View(result);
        }


        // ============================================================
        // EDIT - POST
        // SAVE CORRECTED RESULT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            TestResult model)
        {
            var technician = await _userManager.GetUserAsync(User);

            if (technician == null)
                return Challenge();

            // --------------------------------------------------------
            // VALIDATE ID
            // --------------------------------------------------------

            if (id <= 0)
            {
                TempData["Error"] =
                    "Invalid laboratory result.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // LOAD TEST ITEM
            // --------------------------------------------------------

            var item = await _context.TestRequestItems
                .Include(t => t.TestType)
                .Include(t => t.TestRequest)
                    .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(
                    t => t.TestRequestItemId == id
                );

            if (item == null)
                return NotFound();

            // --------------------------------------------------------
            // ONLY ASSIGNED TECHNICIAN
            // --------------------------------------------------------

            if (item.AssignedTechnicianId != technician.Id)
                return Forbid();

            // --------------------------------------------------------
            // LOAD EXISTING RESULT
            // --------------------------------------------------------

            var existingResult = await _context.TestResults
                .FirstOrDefaultAsync(
                    r => r.TestRequestItemId == id
                );

            if (existingResult == null)
            {
                TempData["Error"] =
                    "The laboratory result could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // CHECK RESULT STATUS
            // --------------------------------------------------------

            if (existingResult.Status != "Completed" &&
                existingResult.Status != "To Be Reviewed")
            {
                TempData["Error"] =
                    "This result can no longer be edited.";

                return RedirectToAction(nameof(Index));
            }

            // --------------------------------------------------------
            // AUTOMATIC UNIT
            // --------------------------------------------------------

            string? automaticUnit =
                item.TestType?.UnitOfMeasurement;

            // --------------------------------------------------------
            // AUTOMATIC REFERENCE RANGE
            // --------------------------------------------------------

            string? referenceRange =
                GetReferenceRange(item);

            // --------------------------------------------------------
            // VALIDATE RESULT
            // --------------------------------------------------------

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(model.ResultValue))
            {
                ModelState.AddModelError(
                    nameof(model.ResultValue),
                    "Please enter a laboratory result."
                );
            }

            if (!ModelState.IsValid)
            {
                model.Units =
                    automaticUnit;

                model.ReferenceRange =
                    referenceRange;

                ViewBag.TestItem =
                    item;

                ViewBag.Patient =
                    item.TestRequest?.Patient;

                ViewBag.IsResubmission =
                    item.Status == "To Be Reviewed";

                ViewBag.PreviousVerificationNote =
                    existingResult.VerificationNote;

                return View(model);
            }

            // --------------------------------------------------------
            // DETERMINE ABNORMAL
            // --------------------------------------------------------

            bool isAbnormal =
                DetermineIfAbnormal(
                    item,
                    model.ResultValue
                );

            // ========================================================
            // UPDATE EXISTING RESULT
            // ========================================================

            existingResult.ResultValue =
                model.ResultValue;

            // IMPORTANT:
            // NEVER use model.Units
            existingResult.Units =
                automaticUnit;

            // IMPORTANT:
            // NEVER use model.ReferenceRange
            existingResult.ReferenceRange =
                referenceRange;

            existingResult.Comments =
                model.Comments;

            existingResult.IsAbnormal =
                isAbnormal;

            existingResult.DateCaptured =
                DateTime.Now;

            existingResult.CapturedByTechnicianId =
                technician.Id;

            // --------------------------------------------------------
            // SEND BACK THROUGH VERIFICATION
            // --------------------------------------------------------

            existingResult.Status =
                "Completed";

            existingResult.VerifiedByTechnicianId =
                null;

            existingResult.VerificationDate =
                null;

            existingResult.VerificationNote =
                null;

            // ========================================================
            // UPDATE TEST ITEM
            // ========================================================

            item.Status =
                "Completed";

            item.CompletionDateTime =
                DateTime.Now;

            // ========================================================
            // SAVE
            // ========================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    "The laboratory result could not be updated: " +
                    (ex.InnerException?.Message ?? ex.Message)
                );

                model.Units =
                    automaticUnit;

                model.ReferenceRange =
                    referenceRange;

                ViewBag.TestItem =
                    item;

                ViewBag.Patient =
                    item.TestRequest?.Patient;

                ViewBag.IsResubmission =
                    item.Status == "To Be Reviewed";

                ViewBag.PreviousVerificationNote =
                    existingResult.VerificationNote;

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred: " +
                    ex.Message
                );

                model.Units =
                    automaticUnit;

                model.ReferenceRange =
                    referenceRange;

                ViewBag.TestItem =
                    item;

                ViewBag.Patient =
                    item.TestRequest?.Patient;

                return View(model);
            }

            // ========================================================
            // SUCCESS
            // ========================================================

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

        private async Task PrepareCaptureView(
            TestRequestItem item)
        {
            ViewBag.TestItem =
                item;

            ViewBag.Patient =
                item.TestRequest?.Patient;

            ViewBag.IsResubmission =
                item.Status == "To Be Reviewed";

            // --------------------------------------------------------
            // FIND EXISTING RESULT
            // --------------------------------------------------------

            var existingResult =
                await _context.TestResults
                    .FirstOrDefaultAsync(
                        r => r.TestRequestItemId ==
                             item.TestRequestItemId
                    );

            ViewBag.PreviousVerificationNote =
                existingResult?.VerificationNote;

            // --------------------------------------------------------
            // AUTOMATIC REFERENCE RANGE
            // --------------------------------------------------------

            ViewBag.ReferenceRange =
                GetReferenceRange(item);

            // --------------------------------------------------------
            // AUTOMATIC UNIT
            // --------------------------------------------------------

            ViewBag.UnitOfMeasurement =
                item.TestType?.UnitOfMeasurement;
        }


        // ============================================================
        // GET REFERENCE RANGE
        // ============================================================

        private string? GetReferenceRange(
            TestRequestItem item)
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

        private bool DetermineIfAbnormal(
            TestRequestItem item,
            string? resultValue)
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

            if (!decimal.TryParse(
                    resultValue,
                    out decimal numericResult))
            {
                return false;
            }

            return
                numericResult <
                item.TestType.ReferenceRangeLow.Value
                ||
                numericResult >
                item.TestType.ReferenceRangeHigh.Value;
        }
    }
}
