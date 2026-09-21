using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public record PatientKey(int PatientID, string Name, string Surname, string IDNumber);

    [Authorize(Roles = "Doctor")]
    public class TestResultController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailAttachmentSender _attachmentSender;
        private readonly IVerifiedResultsPdfGenerator _pdfGenerator;

        public TestResultController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender,
            IEmailAttachmentSender attachmentSender,
            IVerifiedResultsPdfGenerator pdfGenerator)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _attachmentSender = attachmentSender;
            _pdfGenerator = pdfGenerator;
        }

        // GET: /TestResult?abnormalOnly=true&search=kamogelo
        public async Task<IActionResult> Index(bool abnormalOnly = false, string? search = null)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var query = _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(tr => tr.Patient)
                .Include(r => r.VerifiedByTechnician)
                .Where(r => r.TestRequestItem.TestRequest.RequestingDoctorId == doctor.Id);

            if (abnormalOnly)
                query = query.Where(r => r.IsAbnormal);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                query = query.Where(r =>
                    r.TestRequestItem.TestRequest.Patient.Name.ToLower().Contains(term) ||
                    r.TestRequestItem.TestRequest.Patient.Surname.ToLower().Contains(term) ||
                    r.TestRequestItem.TestRequest.Patient.IDNumber.ToLower().Contains(term));
            }

            var results = await query
                .OrderBy(r => r.TestRequestItem.TestRequest.Patient.Surname)
                .ThenByDescending(r => r.DateCaptured)
                .AsNoTracking()
                .ToListAsync();

            var grouped = results
                .GroupBy(r => new PatientKey(
                    r.TestRequestItem.TestRequest.Patient.PatientID,
                    r.TestRequestItem.TestRequest.Patient.Name,
                    r.TestRequestItem.TestRequest.Patient.Surname,
                    r.TestRequestItem.TestRequest.Patient.IDNumber))
                .OrderBy(g => g.Key.Surname)
                .ThenBy(g => g.Key.Name)
                .ToList();

            ViewBag.AbnormalOnly = abnormalOnly;
            ViewBag.Search = search;

            return View(grouped);
        }
        // GET: /TestResult/Alerts?from=2026-09-16&to=2026-09-21
        public async Task<IActionResult> Alerts(DateTime? from = null, DateTime? to = null)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            // Default: last 5 days
            var toDate = to?.Date ?? DateTime.Today;
            var fromDate = from?.Date ?? toDate.AddDays(-5);

            // Ensure from <= to
            if (fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
            }

            // Include the full "to" day (until midnight)
            var toInclusive = toDate.AddDays(1).AddSeconds(-1);

            var alerts = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(tr => tr.Patient)
                .Include(r => r.VerifiedByTechnician)
                .Where(r =>
                    r.IsAbnormal &&
                    r.TestRequestItem.TestRequest.RequestingDoctorId == doctor.Id &&
                    r.DateCaptured >= fromDate &&
                    r.DateCaptured <= toInclusive)
                .OrderByDescending(r => r.DateCaptured)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");
            ViewBag.TotalAlerts = alerts.Count;

            return View(alerts);
        }

        // GET: /TestResult/Request/5
        public async Task<IActionResult> Request(int id)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var results = await _context.TestResults
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItem)
                    .ThenInclude(i => i.TestRequest)
                        .ThenInclude(tr => tr.Patient)
                .Include(r => r.VerifiedByTechnician)
                .Where(r => r.TestRequestItem.TestRequest.RequestId == id &&
                            r.TestRequestItem.TestRequest.RequestingDoctorId == doctor.Id)
                .OrderBy(r => r.TestRequestItem.TestType.Name)
                .AsNoTracking()
                .ToListAsync();

            if (!results.Any()) return NotFound();

            var request = results[0].TestRequestItem.TestRequest;

            // AUTO-PROMOTE: if all items are completed but the parent isn't, fix it
            if (request.Status != "Completed" &&
                request.Status != "Released by doctor" &&
                request.Status != "AppointmentScheduled")
            {
                var items = await _context.TestRequestItems
                    .Where(i => i.RequestId == request.RequestId)
                    .ToListAsync();

                if (items.Any() && items.All(i => i.Status == "Completed"))
                {
                    var trackedRequest = await _context.TestRequests
                        .FirstOrDefaultAsync(r => r.RequestId == request.RequestId);

                    if (trackedRequest != null)
                    {
                        trackedRequest.Status = "Completed";
                        await _context.SaveChangesAsync();
                        request.Status = "Completed";
                    }
                }
            }

            ViewBag.RequestId = id;
            ViewBag.PatientName = $"{request.Patient.Name} {request.Patient.Surname}";
            ViewBag.Status = request.Status;
            ViewBag.AppointmentDate = request.AppointmentDate;
            ViewBag.AppointmentLocation = request.AppointmentLocation;
            ViewBag.AppointmentNote = request.AppointmentNote;
            ViewBag.ReleaseNote = request.ReleaseNote;
            ViewBag.ReleaseDate = request.ReleaseDate;

            return View("RequestResults", results);
        }

        // POST: /TestResult/ScheduleAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleAppointment(
            int requestId,
            DateTime appointmentDate,
            string appointmentLocation,
            string? appointmentNote)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .FirstOrDefaultAsync(r => r.RequestId == requestId &&
                                          r.RequestingDoctorId == doctor.Id);

            if (request == null) return NotFound();

            if (request.Status != "Completed")
            {
                TempData["Error"] = "Only completed results can have an appointment scheduled.";
                return RedirectToAction(nameof(Request), new { id = requestId });
            }

            if (string.IsNullOrWhiteSpace(appointmentLocation))
            {
                TempData["Error"] = "Appointment location is required.";
                return RedirectToAction(nameof(Request), new { id = requestId });
            }

            if (appointmentDate <= DateTime.Now)
            {
                TempData["Error"] = "Appointment date must be in the future.";
                return RedirectToAction(nameof(Request), new { id = requestId });
            }

            request.AppointmentDate = appointmentDate;
            request.AppointmentLocation = appointmentLocation.Trim();
            request.AppointmentNote = appointmentNote?.Trim();
            request.Status = "AppointmentScheduled";

            await _context.SaveChangesAsync();

            var testNames = string.Join(", ",
                request.TestRequestItems.Select(i => i.TestType?.Name ?? "Test"));

            await _emailSender.SendEmailAsync(
                request.Patient.Email,
                "Appointment Scheduled — NMB LAB",
                $@"<p>Dear {request.Patient.Name},</p>
                   <p>Dr. {doctor.LastName} has scheduled an appointment to discuss your results.</p>
                   <p>
                       <strong>Date:</strong> {appointmentDate:dddd, dd MMM yyyy HH:mm}<br/>
                       <strong>Location:</strong> {appointmentLocation}<br/>
                       <strong>Tests:</strong> {testNames}
                   </p>
                   {(string.IsNullOrWhiteSpace(appointmentNote) ? "" : $"<p><strong>Note:</strong> {appointmentNote}</p>")}
                   <p>Please arrive 10 minutes early.</p>");

            TempData["Success"] = "Appointment scheduled and patient emailed.";
            return RedirectToAction(nameof(Request), new { id = requestId });
        }

        // POST: /TestResult/ReleaseResults
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseResults(int requestId, string? releaseNote)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .FirstOrDefaultAsync(r => r.RequestId == requestId &&
                                          r.RequestingDoctorId == doctor.Id);

            if (request == null) return NotFound();

            if (request.Status != "Completed" && request.Status != "AppointmentScheduled")
            {
                TempData["Error"] = "Results can only be released from 'Completed' or 'AppointmentScheduled' status.";
                return RedirectToAction(nameof(Request), new { id = requestId });
            }

            request.Status = "Released by doctor";
            request.ReleaseNote = releaseNote?.Trim();
            request.ReleaseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            var testNames = string.Join(", ",
                request.TestRequestItems.Select(i => i.TestType?.Name ?? "Test"));

            await _emailSender.SendEmailAsync(
                request.Patient.Email,
                "Your Laboratory Results Are Ready",
                $@"<p>Dear {request.Patient.Name},</p>
                   <p>Dr. {doctor.LastName} has released your lab results.</p>
                   <p><strong>Tests:</strong> {testNames}</p>
                   <p><strong>Released on:</strong> {request.ReleaseDate:dd MMM yyyy HH:mm}</p>
                   {(string.IsNullOrWhiteSpace(releaseNote) ? "" : $"<p><strong>Note:</strong> {releaseNote}</p>")}
                   <p>Please log in to the patient portal to view them.</p>");

            TempData["Success"] = "Results released and patient emailed.";
            return RedirectToAction(nameof(Request), new { id = requestId });
        }

        // POST: /TestResult/SendResultsPdf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendResultsPdf(int requestId, string? message)
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null) return Forbid();

            var request = await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.RequestingDoctor)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestType)
                .Include(r => r.TestRequestItems)
                    .ThenInclude(i => i.TestResults)
                        .ThenInclude(res => res.VerifiedByTechnician)
                .FirstOrDefaultAsync(r => r.RequestId == requestId &&
                                          r.RequestingDoctorId == doctor.Id);

            if (request == null) return NotFound();

            if (request.Status != "Completed" &&
                request.Status != "AppointmentScheduled" &&
                request.Status != "Released by doctor")
            {
                TempData["Error"] = "Results must be completed before sending as PDF.";
                return RedirectToAction(nameof(Request), new { id = requestId });
            }

            // Build report data
            var data = new VerifiedResultsReportData
            {
                RequestId = request.RequestId,
                PatientFullName = $"{request.Patient.Name} {request.Patient.Surname}",
                PatientIdNumber = request.Patient.IDNumber,
                RequestingDoctorFullName = request.RequestingDoctor != null
                    ? $"Dr. {request.RequestingDoctor.FirstName} {request.RequestingDoctor.LastName}"
                    : null,
                GeneratedAt = DateTime.Now,
                Rows = request.TestRequestItems
                    .SelectMany(i => i.TestResults.Select(r => new VerifiedResultRow
                    {
                        TestName = i.TestType?.Name ?? "Test",
                        ResultValue = r.ResultValue,
                        Units = r.Units,
                        ReferenceRange = r.ReferenceRange,
                        IsAbnormal = r.IsAbnormal,
                        Comments = r.Comments,
                        VerifiedByFullName = r.VerifiedByTechnician != null
                            ? $"{r.VerifiedByTechnician.FirstName} {r.VerifiedByTechnician.LastName}"
                            : null,
                        VerificationDate = r.VerificationDate
                    }))
                    .ToList()
            };

            // Generate the PDF
            var pdfBytes = _pdfGenerator.Generate(data);

            // Email it
            var htmlBody = $@"
                <p>Dear {request.Patient.Name},</p>
                <p>{(string.IsNullOrWhiteSpace(message)
                    ? "Please find your laboratory results attached to this email."
                    : message)}</p>
                <p>
                    <strong>Request Number:</strong> TR-{request.RequestId}<br/>
                    <strong>Tests:</strong> {data.Rows.Count}
                </p>
                <p>If you have any questions, please contact NMB LAB.</p>";

            await _attachmentSender.SendEmailWithAttachmentAsync(
                request.Patient.Email,
                $"Laboratory Results — TR-{request.RequestId}",
                htmlBody,
                pdfBytes,
                $"LabResults_TR-{request.RequestId}.pdf");

            TempData["Success"] = "Results PDF emailed to the patient.";
            return RedirectToAction(nameof(Request), new { id = requestId });
        }
    }
}