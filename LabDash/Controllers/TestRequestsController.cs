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
                // Reload dropdown data before returning the view with errors
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

            // 1. Create the TestRequest header
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
            await _context.SaveChangesAsync(); // save now so testRequest.RequestId is populated

            // 2. Create one TestRequestItem per selected test type
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

            // 3. Create placeholder Sample rows, one per distinct required sample type
            var distinctSampleTypes = selectedTypes
                .Select(t => t.RequiredSampleType)
                .Distinct();

            foreach (var sampleType in distinctSampleTypes)
            {
                _context.Samples.Add(new Sample
                {
                    TestRequestId = testRequest.RequestId,
                    SampleType = sampleType,
                    Barcode = string.Empty,      // filled in later by technician
                    IsReceived = false
                });
            }

            await _context.SaveChangesAsync();

            // 4. Notify the patient by email
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
    }
}