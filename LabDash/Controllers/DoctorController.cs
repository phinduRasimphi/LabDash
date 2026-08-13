using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabDash.ViewModels;   // ← this one


// ...etc
using System.Security.Cryptography;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly IEmailSender _emailSender;

        public DoctorController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: /Doctor/ManagePatients
        // GET: /Doctor/ManagePatients?searchIDNumber=xxxxx
        public async Task<IActionResult> ManagePatients(string? searchIDNumber)
        {
            var vm = new ManagePatientsViewModel
            {
                SearchIDNumber = searchIDNumber
            };

            if (!string.IsNullOrWhiteSpace(searchIDNumber))
            {
                vm.HasSearched = true;

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IDNumber == searchIDNumber);

                if (patient != null)
                {
                    vm.SearchResult = new PatientDetailsViewModel
                    {
                        PatientID = patient.PatientID,
                        UserId = patient.UserId,
                        Name = patient.Name,
                        Surname = patient.Surname,
                        IDNumber = patient.IDNumber,
                        CellphoneNumber = patient.CellphoneNumber,
                        DOB = patient.DOB,
                        Email = patient.Email,
                        HomeAddress = patient.HomeAddress,
                        MedicalConditions = patient.MedicalConditions,
                        Allergies = patient.Allergies,
                        Medication = patient.Medication
                    };
                }
            }

            return View(vm);
        }

        // POST: /Doctor/CreatePatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(PatientCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(ManagePatients));
            }

            // Prevent duplicate SA ID numbers
            bool idExists = await _context.Patients.AnyAsync(p => p.IDNumber == model.IDNumber);
            if (idExists)
            {
                TempData["Error"] = "A patient with this ID number already exists.";
                return RedirectToAction(nameof(ManagePatients));
            }

            // 1. Generate a secure temporary password
            string generatedPassword = GenerateTemporaryPassword();

            // 2. Create the login account (LabUser)
            var user = new LabUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Name,
                LastName = model.Surname,
                PhoneNumb = model.CellphoneNumber,
                MustChangePassword = true
            };

            var createResult = await _userManager.CreateAsync(user, generatedPassword);
            if (!createResult.Succeeded)
            {
                TempData["Error"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ManagePatients));
            }

            // Assign the Patient role
            await _userManager.AddToRoleAsync(user, "Patient");

            // 3. Create the Patient record, linked via UserId
            var patient = new Patient
            {
                UserId = user.Id,
                Name = model.Name,
                Surname = model.Surname,
                IDNumber = model.IDNumber,
                CellphoneNumber = model.CellphoneNumber,
                DOB = model.DOB,
                Email = model.Email,
                HomeAddress = model.HomeAddress,
                MedicalConditions = model.MedicalConditions,
                Allergies = model.Allergies,
                Medication = model.Medication
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // 4. Email the temporary password
            string loginUrl = Url.Action("Login", "Account", null, Request.Scheme);
            string emailBody = $@"
                <p>Hi {model.Name},</p>
                <p>An account has been created for you at NMB LAB.</p>
                <p><strong>Username:</strong> {model.Email}<br/>
                <strong>Temporary Password:</strong> {generatedPassword}</p>
                <p>You will be required to change this password when you first log in.</p>
                <p><a href='{loginUrl}'>Click here to log in</a></p>";

            await _emailSender.SendEmailAsync(model.Email, "Your NMB LAB account", emailBody);

            TempData["Success"] = "Patient created successfully. Login details have been emailed.";
            return RedirectToAction(nameof(ManagePatients));
        }

        // POST: /Doctor/UpdatePatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePatient(PatientDetailsViewModel model)
        {
            var patient = await _context.Patients.FindAsync(model.PatientID);
            if (patient == null) return NotFound();

            patient.MedicalConditions = model.MedicalConditions;
            patient.Allergies = model.Allergies;
            patient.Medication = model.Medication;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Patient record updated.";
            return RedirectToAction(nameof(ManagePatients));
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
            var bytes = RandomNumberGenerator.GetBytes(12);
            var result = new char[12];
            for (int i = 0; i < 12; i++)
                result[i] = chars[bytes[i] % chars.Length];
            return new string(result);
        }
    }
}