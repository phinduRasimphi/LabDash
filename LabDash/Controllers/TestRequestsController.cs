using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class TestRequestController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IEmailSender _emailSender;

        public TestRequestController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: /TestRequest/Create?patientId=5
        public async Task<IActionResult> Create(int? patientId)
        {
            if (patientId == null)
            {
                TempData["Error"] = "No patient selected. Please search for a patient first.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient == null)
            {
                TempData["Error"] = "Patient not found.";
                return RedirectToAction("ManagePatients", "Doctor");
            }

            var testTypes = await _context.TestTypes
                .OrderBy(t => t.Category).ThenBy(t => t.Name)
                .Select(t => new TestTypeOptionViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Category = t.Category,
                    RequiredSampleType = t.RequiredSampleType,
                    TurnaroundTimeHours = t.TurnaroundTimeHours
                })
                .ToListAsync();

            var vm = new TestRequestCreateViewModel
            {
                PatientId = patient.PatientID,
                PatientName = $"{patient.Name} {patient.Surname}",
                PatientIDNumber = patient.IDNumber,
                AvailableTestTypes = testTypes
            };

            return View(vm);
        }

        // POST: /TestRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestRequestCreateViewModel model)
        {
            if (model.SelectedTestTypeIds == null || !model.SelectedTestTypeIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedTestTypeIds), "Select at least one test type");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableTestTypes = await _context.TestTypes
                    .OrderBy(t => t.Category).ThenBy(t => t.Name)
                    .Select(t => new TestTypeOptionViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Category = t.Category,
                        RequiredSampleType = t.RequiredSampleType,
                        TurnaroundTimeHours = t.TurnaroundTimeHours
                    })
                    .ToListAsync();

                var patientForReload = await _context.Patients.FindAsync(model.PatientId);
                if (patientForReload != null)
                {
                    model.PatientName = $"{patientForReload.Name} {patientForReload.Surname}";
                    model.PatientIDNumber = patientForReload.IDNumber;
                }

                return View(model);
            }

            var doctor = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FindAsync(model.PatientId);
            if (patient == null) return NotFound();

            var testRequest = new TestRequest
            {
                PatientId = model.PatientId,
                RequestingDoctorId = doctor.Id,
                RequestDate = model.RequestDate,
                Urgency = model.Urgency,
                ClinicalNotes = model.ClinicalNotes,
                Status = "Submitted",
                SubmittedDate = DateTime.Now
            };

            _context.TestRequests.Add(testRequest);
            await _context.SaveChangesAsync();

            var selectedTypes = await _context.TestTypes
                .Where(t => model.SelectedTestTypeIds.Contains(t.Id))
                .ToListAsync();

            foreach (var type in selectedTypes)
            {
                _context.TestRequestItems.Add(new TestRequestItem
                {
                    RequestId = testRequest.RequestId,
                    TestTypeId = type.Id,
                    Status = "Submitted"
                });
            }

            await _context.SaveChangesAsync();

            string testListHtml = string.Join(", ", selectedTypes.Select(t => t.Name));
            string emailBody = $@"
                <p>Dear {patient.Name},</p>
                <p>Dr. {doctor.LastName} has submitted a test request for you at NMB LAB.</p>
                <p><strong>Tests requested:</strong> {testListHtml}<br/>
                <strong>Date:</strong> {testRequest.RequestDate:dd MMM yyyy}</p>
                <p>Please visit the lab at your earliest convenience to provide the required samples.</p>";

            await _emailSender.SendEmailAsync(patient.Email, "New Test Request Submitted", emailBody);

            TempData["Success"] = "Test request submitted successfully. The patient has been notified.";
            return RedirectToAction("ManagePatients", "Doctor");
        }

        // GET: /TestRequest/Track
        public async Task<IActionResult> Track()
        {
            var doctor = await _userManager.GetUserAsync(User);

            var folders = await _context.TestRequests
                .Where(r => r.RequestingDoctorId == doctor.Id)
                .GroupBy(r => r.Patient)
                .Select(g => new PatientFolderSummaryViewModel
                {
                    PatientId = g.Key.PatientID,
                    PatientName = g.Key.Name + " " + g.Key.Surname,
                    PatientIDNumber = g.Key.IDNumber,
                    RequestCount = g.Count()
                })
                .OrderBy(p => p.PatientName)
                .ToListAsync();

            var vm = new TrackRequestsViewModel { PatientFolders = folders };
            return View(vm);
        }

        // POST: /TestRequest/UnlockFolder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFolder(int patientId, string idNumber)
        {
            var doctor = await _userManager.GetUserAsync(User);

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            if (patient.IDNumber != idNumber?.Trim())
            {
                TempData["Error"] = "Incorrect ID number. Folder remains locked.";
                return RedirectToAction(nameof(Track));
            }

            var requests = await _context.TestRequests
                .Where(r => r.PatientId == patientId && r.RequestingDoctorId == doctor.Id)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new TestRequestTrackViewModel
                {
                    RequestId = r.RequestId,
                    RequestDate = r.RequestDate,
                    Urgency = r.Urgency,
                    Status = r.Status,
                    TestTypeNames = r.TestRequestItems.Select(i => i.TestType.Name).ToList()
                })
                .ToListAsync();

            var vm = new PatientRequestsViewModel
            {
                PatientName = $"{patient.Name} {patient.Surname}",
                PatientIDNumber = patient.IDNumber,
                Requests = requests
            };

            return View("PatientRequests", vm);
        }

        // POST: /TestRequest/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int requestId, string cancellationReason)
        {
            var doctor = await _userManager.GetUserAsync(User);

            var request = await _context.TestRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            if (request.RequestingDoctorId != doctor.Id) return Forbid();

            if (request.Status != "Submitted" && request.Status != "Samples Received")
            {
                TempData["Error"] = "This request can no longer be cancelled.";
                return RedirectToAction(nameof(Track));
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] = "A cancellation reason is required.";
                return RedirectToAction(nameof(Track));
            }

            request.Status = "Cancelled";
            request.CancellationReason = cancellationReason;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Test request cancelled.";
            return RedirectToAction(nameof(Track));
        }

        // GET: /TestRequest/Results
        public async Task<IActionResult> Results()
        {
            var doctor = await _userManager.GetUserAsync(User);

            var folders = await _context.TestRequests
                .Where(r => r.RequestingDoctorId == doctor.Id
                         && (r.Status == "Completed" || r.Status == "Released"))
                .GroupBy(r => r.Patient)
                .Select(g => new ResultsFolderSummaryViewModel
                {
                    PatientId = g.Key.PatientID,
                    PatientName = g.Key.Name + " " + g.Key.Surname,
                    PatientIDNumber = g.Key.IDNumber,
                    CompletedRequestCount = g.Count(),
                    HasAbnormalResult = g.Any(r => r.TestRequestItems
                        .Any(i => _context.TestResults
                            .Any(res => res.TestRequestItemId == i.TestRequestItemId && res.IsAbnormal)))
                })
                .OrderBy(p => p.PatientName)
                .ToListAsync();

            return View(new ResultsFolderListViewModel { PatientFolders = folders });
        }

        // POST: /TestRequest/UnlockResultsFolder
        // POST: /TestRequest/UnlockResultsFolder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockResultsFolder(
            int patientId,
            string idNumber)
        {
            var doctor = await _userManager.GetUserAsync(User);

            if (doctor == null)
            {
                return Unauthorized();
            }

            var patient = await _context.Patients.FindAsync(patientId);

            if (patient == null)
            {
                return NotFound();
            }

            if (patient.IDNumber != idNumber?.Trim())
            {
                TempData["Error"] = "Incorrect ID number. Folder remains locked.";
                return RedirectToAction(nameof(Results));
            }

            var requests = await _context.TestRequests
                .Where(r =>
                    r.PatientId == patientId &&
                    r.RequestingDoctorId == doctor.Id &&
                    (r.Status == "Completed" || r.Status == "Released"))
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var vm = new PatientResultsViewModel
            {
                PatientName = $"{patient.Name} {patient.Surname}",
                PatientIDNumber = patient.IDNumber,
                Requests = new List<TestRequestResultsViewModel>()
            };

            foreach (var r in requests)
            {
                var items = await _context.TestRequestItems
                    .Include(i => i.TestType)
                    .Where(i => i.RequestId == r.RequestId)
                    .ToListAsync();

                var results = new List<TestResultLineViewModel>();

                foreach (var item in items)
                {
                    var result = await _context.TestResults
                        .FirstOrDefaultAsync(
                            res => res.TestRequestItemId == item.TestRequestItemId);

                    if (result != null)
                    {
                        results.Add(new TestResultLineViewModel
                        {
                            TestTypeName = item.TestType.Name,
                            ResultValue = result.ResultValue,
                            Units = result.Units,
                            ReferenceRange = result.ReferenceRange,
                            IsAbnormal = result.IsAbnormal
                        });
                    }
                }

                vm.Requests.Add(new TestRequestResultsViewModel
                {
                    RequestId = r.RequestId,
                    RequestDate = r.RequestDate,
                    DoctorName = doctor.LastName,
                    Urgency = r.Urgency,
                    ClinicalNotes = r.ClinicalNotes,
                    Status = r.Status,
                    ReleaseNote = r.ReleaseNote,
                    Results = results
                });
            }

            return View("PatientResults", vm);
        }

        // POST: /TestRequest/ReleaseResults
        [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ReleaseResults(int requestId, string releaseNote)
            {
                var doctor = await _userManager.GetUserAsync(User);

                var request = await _context.TestRequests
                    .Include(r => r.Patient)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null) return NotFound();
                if (request.RequestingDoctorId != doctor.Id) return Forbid();

                if (request.Status != "Completed")
                {
                    TempData["Error"] = "Only completed results can be released.";
                    return RedirectToAction(nameof(Results));
                }

                request.Status = "Released";
                request.ReleaseNote = releaseNote;
                request.ReleaseDate = DateTime.Now;

                await _context.SaveChangesAsync();

                string emailBody = $@"
                <p>Dear {request.Patient.Name},</p>
                <p>Your test results from Dr. {doctor.LastName} are now available.</p>
                {(string.IsNullOrWhiteSpace(releaseNote) ? "" : $"<p>{releaseNote}</p>")}
                <p>Please contact the lab or your doctor if you have any questions.</p>";

                await _emailSender.SendEmailAsync(request.Patient.Email, "Your test results are available", emailBody);

            TempData["Success"] = "Results released to patient.";
            return RedirectToAction(nameof(Results));
        }
    }
}