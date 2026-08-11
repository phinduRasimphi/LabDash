using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LabDash.Controllers
{
    public class AdminController : Controller
    {
        private readonly LabDbContext _context;

        public AdminController(LabDbContext context)
        {
            _context = context;
        }

        private void SetSidebarData(string activePage)
        {
            ViewData["ActivePage"] = activePage;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            SetSidebarData("Dashboard");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var vm = new AdminDashboardViewModel
            {
                // ==============================================
                // ✅ YOUR EXISTING ADMIN DATA — UNCHANGED
                // ==============================================
                ConditionCount = _context.MedicalConditions.Count(x => x.IsActive),
                AllergyCount = _context.Allergies.Count(x => x.IsActive),
                MedicationCount = _context.Medications.Count(x => x.IsActive),
                UserCount = _context.Users.Count(),

                RecentConditions = _context.MedicalConditions
                    .Include(x => x.Category)
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.MedicalConditionId)
                    .Take(5)
                    .ToList(),

                RecentMedications = _context.Medications
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.MedicationId)
                    .Take(5)
                    .ToList()
            };

            // ==============================================
            // ✅ PATIENT DATA — NOW INSIDE THE METHOD!
            // ==============================================
            if (!string.IsNullOrEmpty(userId))
            {
                var patient = _context.Patients.FirstOrDefault(p => p.UserId == userId);

                if (patient != null)
                {
                    vm.PatientProfile = new PatientProfileViewModel
                    {
                        PatientID = patient.PatientID,
                        Name = patient.Name ?? "",
                        Surname = patient.Surname ?? "",
                        IDNumber = patient.IDNumber ?? "",
                        DateOfBirth = patient.DOB 
,
                        Cellphone = patient.CellphoneNumber ?? "",
                        Email = patient.Email ?? "",
                        HomeAddress = patient.HomeAddress ?? ""
                    };

                    var patientRequests = _context.TestRequests
                        .Where(r => r.PatientId == patient.PatientID)
                        .OrderByDescending(r => r.RequestDate)
                        .ToList();

                    vm.PatientTotalRequests = patientRequests.Count;
                    vm.PatientPendingRequests = patientRequests.Count(r =>
                        r.Status == "Submitted" || r.Status == "Samples Received");
                    vm.PatientResultsReady = patientRequests.Count(r =>
                        r.Status == "Completed" || r.Status == "Released");

                    var patientRequestIds = patientRequests.Select(r => r.PatientId).ToList();
                    vm.PatientAbnormalCount = _context.TestResults
                        .Include(r => r.TestRequestItem)
                        .Where(r => patientRequestIds.Contains(r.TestRequestItem.RequestId) && r.IsAbnormal)
                        .Count();

                    vm.PatientRecentRequests = patientRequests
                        .Take(5)
                        .Select(r => new TestRequestViewModel
                        {
                            RequestID = r.RequestId.ToString(),
                            RequestDate = r.RequestDate,
                            DoctorName = r.RequestingDoctorId ?? "N/A",
                            Urgency = r.Urgency ?? "Routine",
                            Status = r.Status ?? "Submitted",
                            Tests = new List<string>()
                        })
                        .ToList();
                }
            }

            return View(vm);
        }

        // ===========================
        // CONDITIONS
        // ===========================

        public IActionResult Conditions()
        {
            SetSidebarData("Conditions");

            var vm = new AdminListViewModel
            {
                PageTitle = "Conditions",

                Categories = _context.Categories
                    .Where(c => c.Type == "Condition" && c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToList(),

                Conditions = _context.MedicalConditions
                    .Include(x => x.Category)
                    .Where(x => x.IsActive)
                    .ToList(),

                InactiveConditions = _context.MedicalConditions
                    .Include(x => x.Category)
                    .Where(x => !x.IsActive)
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult AddCondition(string name, int categoryId, string? description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var condition = new MedicalCondition
                {
                    ConditionName = name,
                    CategoryId = categoryId,
                    Description = description,
                    IsActive = true
                };

                _context.MedicalConditions.Add(condition);
                _context.SaveChanges();

                TempData["Success"] = "Condition added successfully.";
            }

            return RedirectToAction(nameof(Conditions));
        }

        [HttpPost]
        public IActionResult EditCondition(int id, string name, int categoryId, string? description)
        {
            var condition = _context.MedicalConditions.Find(id);

            if (condition != null)
            {
                condition.ConditionName = name;
                condition.CategoryId = categoryId;
                condition.Description = description;

                _context.SaveChanges();
                TempData["Success"] = "Condition updated successfully.";
            }

            return RedirectToAction(nameof(Conditions));
        }

        [HttpPost]
        public IActionResult DeleteCondition(int id)
        {
            var condition = _context.MedicalConditions.Find(id);

            if (condition != null)
            {
                condition.IsActive = false;
                _context.SaveChanges();
                TempData["Success"] = "Condition deactivated.";
            }

            return RedirectToAction(nameof(Conditions));
        }

        [HttpPost]
        public IActionResult AddCategory(string name, string type)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                bool exists = _context.Categories
                    .Any(c => c.Name == name && c.Type == type);

                if (!exists)
                {
                    _context.Categories.Add(new Category
                    {
                        Name = name,
                        Type = type,
                        IsActive = true
                    });
                    _context.SaveChanges();
                    TempData["Success"] = "Category added.";
                }
                else
                {
                    TempData["Error"] = "That category already exists.";
                }
            }

            return RedirectToAction(nameof(Conditions));
        }

        private static readonly List<string> _defaultAllergyCategories = new()
        {
            "Drug Allergies",
            "Food Allergies",
            "Environmental"
        };

        // ===========================
        // ALLERGIES
        // ===========================

        public IActionResult Allergies()
        {
            SetSidebarData("Allergies");

            var usedCategories = _context.Allergies
                .Select(a => a.Category)
                .Distinct()
                .ToList();

            var allCategories = _defaultAllergyCategories
                .Union(usedCategories)
                .OrderBy(c => c)
                .ToList();

            var vm = new AllergyListViewModel
            {
                PageTitle = "Allergies",
                Categories = allCategories,

                Allergies = _context.Allergies
                    .Where(x => x.IsActive)
                    .ToList(),

                InactiveAllergies = _context.Allergies
                    .Where(x => !x.IsActive)
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult AddAllergy(string name, string category, string? description)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(category))
            {
                var allergy = new Allergy
                {
                    AllergyName = name,
                    Category = category,
                    Description = description,
                    IsActive = true
                };

                _context.Allergies.Add(allergy);
                _context.SaveChanges();

                TempData["Success"] = "Allergy added successfully.";
            }

            return RedirectToAction(nameof(Allergies));
        }

        [HttpPost]
        public IActionResult EditAllergy(int id, string name, string category, string? description)
        {
            var allergy = _context.Allergies.Find(id);

            if (allergy != null)
            {
                allergy.AllergyName = name;
                allergy.Category = category;
                allergy.Description = description;

                _context.SaveChanges();
                TempData["Success"] = "Allergy updated successfully.";
            }

            return RedirectToAction(nameof(Allergies));
        }

        [HttpPost]
        public IActionResult ReactivateAllergy(int id)
        {
            var allergy = _context.Allergies.Find(id);

            if (allergy != null)
            {
                allergy.IsActive = true;
                _context.SaveChanges();
                TempData["Success"] = "Allergy reactivated.";
            }

            return RedirectToAction(nameof(Allergies));
        }

        [HttpPost]
        public IActionResult DeleteAllergy(int id)
        {
            var allergy = _context.Allergies.Find(id);

            if (allergy != null)
            {
                allergy.IsActive = false;
                _context.SaveChanges();
                TempData["Success"] = "Allergy deactivated.";
            }

            return RedirectToAction(nameof(Allergies));
        }

        private static readonly List<string> _defaultMedicationCategories = new()
        {
            "Antibiotics",
            "Pain Relief",
            "Chronic Condition"
        };

        // ===========================
        // MEDICATIONS
        // ===========================

        public IActionResult Medications()
        {
            SetSidebarData("Medications");

            var usedCategories = _context.Medications
                .Select(m => m.Category)
                .Distinct()
                .ToList();

            var allCategories = _defaultMedicationCategories
                .Union(usedCategories)
                .OrderBy(c => c)
                .ToList();

            var vm = new MedicationListViewModel
            {
                PageTitle = "Medications",
                Categories = allCategories,

                Medications = _context.Medications
                    .Where(x => x.IsActive)
                    .ToList(),

                InactiveMedications = _context.Medications
                    .Where(x => !x.IsActive)
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult AddMedication(string name, string category, string? description)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(category))
            {
                var medication = new Medication
                {
                    MedicationName = name,
                    Category = category,
                    Description = description,
                    IsActive = true
                };

                _context.Medications.Add(medication);
                _context.SaveChanges();

                TempData["Success"] = "Medication added successfully.";
            }

            return RedirectToAction(nameof(Medications));
        }

        [HttpPost]
        public IActionResult EditMedication(int id, string name, string category, string? description)
        {
            var medication = _context.Medications.Find(id);

            if (medication != null)
            {
                medication.MedicationName = name;
                medication.Category = category;
                medication.Description = description;

                _context.SaveChanges();
                TempData["Success"] = "Medication updated successfully.";
            }

            return RedirectToAction(nameof(Medications));
        }

        [HttpPost]
        public IActionResult ReactivateMedication(int id)
        {
            var medication = _context.Medications.Find(id);

            if (medication != null)
            {
                medication.IsActive = true;
                _context.SaveChanges();
                TempData["Success"] = "Medication reactivated.";
            }

            return RedirectToAction(nameof(Medications));
        }

        [HttpPost]
        public IActionResult DeleteMedication(int id)
        {
            var medication = _context.Medications.Find(id);

            if (medication != null)
            {
                medication.IsActive = false;
                _context.SaveChanges();
                TempData["Success"] = "Medication deactivated.";
            }

            return RedirectToAction(nameof(Medications));
        }

        public IActionResult AuditLog()
        {
            SetSidebarData("AuditLog");

            var vm = new AuditLogViewModel
            {
                Entries = _context.AuditLogs
                    .OrderByDescending(x => x.ActionDate)
                    .Select(x => new AuditEntry
                    {
                        Timestamp = x.ActionDate.ToString("g"),
                        User = x.UserName,
                        Role = "",
                        Action = x.Action,
                        Details = $"{x.TableName} — {x.Details}"
                    })
                    .ToList()
            };

            return View(vm);
        }

        public IActionResult SystemTables()
        {
            SetSidebarData("SystemTables");

            var vm = new SystemTablesViewModel
            {
                SampleTypes = _context.SampleTypeLookups.OrderBy(x => x.Name).ToList(),
                Units = _context.Units.OrderBy(x => x.Name).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult AddSampleType(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.SampleTypeLookups.Add(new SampleTypeLookup { Name = name });
                _context.SaveChanges();
                TempData["Success"] = "Sample type added.";
            }

            return RedirectToAction(nameof(SystemTables));
        }

        [HttpPost]
        public IActionResult DeleteSampleType(int id)
        {
            var item = _context.SampleTypeLookups.Find(id);

            if (item != null)
            {
                _context.SampleTypeLookups.Remove(item);
                _context.SaveChanges();
                TempData["Success"] = "Sample type deleted.";
            }

            return RedirectToAction(nameof(SystemTables));
        }

        [HttpPost]
        public IActionResult AddUnit(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.Units.Add(new Unit { Name = name });
                _context.SaveChanges();
                TempData["Success"] = "Unit added.";
            }

            return RedirectToAction(nameof(SystemTables));
        }

        [HttpPost]
        public IActionResult DeleteUnit(int id)
        {
            var item = _context.Units.Find(id);

            if (item != null)
            {
                _context.Units.Remove(item);
                _context.SaveChanges();
                TempData["Success"] = "Unit deleted.";
            }

            return RedirectToAction(nameof(SystemTables));
        }
    }
}