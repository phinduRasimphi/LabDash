using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            var vm = new TestRequestViewModel
            {
                PatientId = patient.PatientID,
                PatientName = patient.Name,
                PatientSurname = patient.Surname,
                PatientIDNumber = patient.IDNumber,
                PatientDOB = patient.DOB,
                PatientCellphone = patient.CellphoneNumber,
                PatientEmail = patient.Email,
                MedicalConditions = patient.MedicalConditions,
                Allergies = patient.Allergies,
                Medication = patient.Medication,
                AvailableTestTypes = await GetTestTypeSelectListAsync()
            };

            return View(vm);
        }

        // POST: /TestRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var patientForReload = await _context.Patients.FindAsync(model.PatientId);
                if (patientForReload != null)
                {
                    model.PatientName = patientForReload.Name;
                    model.PatientSurname = patientForReload.Surname;
                    model.PatientIDNumber = patientForReload.IDNumber;
                    model.PatientDOB = patientForReload.DOB;
                    model.PatientCellphone = patientForReload.CellphoneNumber;
                    model.PatientEmail = patientForReload.Email;
                    model.MedicalConditions = patientForReload.MedicalConditions;
                    model.Allergies = patientForReload.Allergies;
                    model.Medication = patientForReload.Medication;
                }

                model.AvailableTestTypes = await GetTestTypeSelectListAsync();
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

            // Save each entered barcode as a Sample tied to this request
            foreach (var barcode in model.Barcode.Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                _context.Samples.Add(new Sample
                {
                    TestRequestId = testRequest.RequestId,
                    Barcode = barcode.Trim(),
                    IsReceived = false
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

        private async Task<List<SelectListItem>> GetTestTypeSelectListAsync()
        {
            return await _context.TestTypes
                .OrderBy(t => t.Category).ThenBy(t => t.Name)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name + " (" + t.RequiredSampleType + ")"
                })
                .ToListAsync();
        }

        // ... Track, UnlockFolder, Cancel, Results, UnlockResultsFolder, ReleaseResults
        // stay exactly as they already are — unaffected by this change
    }
}