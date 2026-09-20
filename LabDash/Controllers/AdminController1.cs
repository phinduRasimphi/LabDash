using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace LabDash.Controllers
{
    public class AdminController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;
        private readonly SignInManager<LabUser> _signInManager;
        public AdminController(
            LabDbContext context,
            UserManager<LabUser> userManager,
            SignInManager<LabUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        // ==========================================================
        // SIDEBAR
        // ==========================================================
        private void SetSidebarData(string activePage)
        {
            ViewData["ActivePage"] = activePage;
        }
        // ==========================================================
        // AUDIT HELPER
        // ==========================================================
        private void AddAuditLog(
            string action,
            string tableName,
            string details)
        {
            var userName =
                User.Identity?.Name ?? "System";
            var auditLog = new AuditLog
            {
                ActionDate = DateTime.Now,
                UserName = userName,
                Action = action,
                TableName = tableName,
                Details = details
            };
            _context.AuditLogs.Add(auditLog);
        }
        private string GetCurrentUserName()
        {
            return User.Identity?.Name ?? "System";
        }
        // ==========================================================
        // INDEX
        // ==========================================================
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }
        // ==========================================================
        // DASHBOARD
        // ==========================================================
        public IActionResult Dashboard()
        {
            SetSidebarData("Dashboard");
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vm = new AdminDashboardViewModel
            {
                ConditionCount =
                    _context.MedicalConditions
                        .Count(x => x.IsActive),
                AllergyCount =
                    _context.Allergies
                        .Count(x => x.IsActive),
                MedicationCount =
                    _context.Medications
                        .Count(x => x.IsActive),
                UserCount =
                    _context.Users.Count(),
                RecentConditions =
                    _context.MedicalConditions
                        .Include(x => x.Category)
                        .Where(x => x.IsActive)
                        .OrderByDescending(
                            x => x.MedicalConditionId)
                        .Take(5)
                        .ToList(),
                RecentMedications =
                    _context.Medications
                        .Include(x => x.Category)
                        .Where(x => x.IsActive)
                        .OrderByDescending(
                            x => x.MedicationId)
                        .Take(5)
                        .ToList()
            };
            if (!string.IsNullOrEmpty(userId))
            {
                var patient =
                    _context.Patients
                        .FirstOrDefault(
                            p => p.UserId == userId);
                if (patient != null)
                {
                    vm.PatientProfile =
                        new PatientProfileViewModel
                        {
                            PatientID =
                                patient.PatientID,
                            Name =
                                patient.Name ?? "",
                            Surname =
                                patient.Surname ?? "",
                            IDNumber =
                                patient.IDNumber ?? "",
                            DateOfBirth =
                                patient.DOB,
                            Cellphone =
                                patient.CellphoneNumber ?? "",
                            Email =
                                patient.Email ?? "",
                            HomeAddress =
                                patient.HomeAddress ?? ""
                        };
                    var patientRequests =
                        _context.TestRequests
                            .Where(r =>
                                r.PatientId ==
                                patient.PatientID)
                            .OrderByDescending(
                                r => r.RequestDate)
                            .ToList();
                    vm.PatientTotalRequests =
                        patientRequests.Count;
                    vm.PatientPendingRequests =
                        patientRequests.Count(r =>
                            r.Status == "Submitted" ||
                            r.Status == "Samples Received");
                    vm.PatientResultsReady =
                        patientRequests.Count(r =>
                            r.Status == "Completed" ||
                            r.Status == "Released");
                    var patientRequestIds =
                        patientRequests
                            .Select(r => r.RequestId)
                            .ToList();
                    vm.PatientAbnormalCount =
                        _context.TestResults
                            .Include(r =>
                                r.TestRequestItem)
                            .Where(r =>
                                patientRequestIds.Contains(
                                    r.TestRequestItem.RequestId)
                                &&
                                r.IsAbnormal)
                            .Count();
                    vm.PatientRecentRequests =
                        patientRequests
                            .Take(5)
                            .Select(r =>
                                new TestRequestViewModel
                                {
                                    RequestID =
                                        r.RequestId.ToString(),
                                    RequestDate =
                                        r.RequestDate,
                                    DoctorName =
                                        r.RequestingDoctorId
                                        ?? "N/A",
                                    Urgency =
                                        r.Urgency
                                        ?? "Routine",
                                    Status =
                                        r.Status
                                        ?? "Submitted",
                                    Tests =
                                        new List<string>()
                                })
                            .ToList();
                }
            }
            return View(vm);
        }
        // ==========================================================
        // CONDITIONS
        // ==========================================================
        public IActionResult Conditions()
        {
            SetSidebarData("Conditions");
            var vm = new AdminListViewModel
            {
                PageTitle = "Conditions",
                Categories =
                    _context.Categories
                        .Where(c =>
                            c.Type == "MedicalCondition" &&
                            c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList(),
                InactiveCategories =
                    _context.Categories
                        .Where(c =>
                            c.Type == "MedicalCondition" &&
                            !c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList(),
                Conditions =
                    _context.MedicalConditions
                        .Include(x => x.Category)
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.ConditionName)
                        .ToList(),
                InactiveConditions =
                    _context.MedicalConditions
                        .Include(x => x.Category)
                        .Where(x => !x.IsActive)
                        .OrderBy(x => x.ConditionName)
                        .ToList()
            };
            return View(vm);
        }
        [HttpPost]
        public IActionResult AddCondition(
            string name,
            int categoryId,
            string? description)
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
                AddAuditLog(
                    "Created",
                    "Medical Conditions",
                    $"Added condition '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Condition added successfully.";
            }
            return RedirectToAction(nameof(Conditions));
        }
        [HttpPost]
        public IActionResult EditCondition(
            int id,
            string name,
            int categoryId,
            string? description)
        {
            var condition =
                _context.MedicalConditions.Find(id);
            if (condition != null)
            {
                var oldName =
                    condition.ConditionName;
                condition.ConditionName = name;
                condition.CategoryId = categoryId;
                condition.Description = description;
                AddAuditLog(
                    "Updated",
                    "Medical Conditions",
                    $"Updated condition '{oldName}' to '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Condition updated successfully.";
            }
            return RedirectToAction(nameof(Conditions));
        }
        [HttpPost]
        public IActionResult DeleteCondition(int id)
        {
            var condition =
                _context.MedicalConditions.Find(id);
            if (condition != null)
            {
                condition.IsActive = false;
                AddAuditLog(
                    "Deactivated",
                    "Medical Conditions",
                    $"Deactivated condition '{condition.ConditionName}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Condition deactivated.";
            }
            return RedirectToAction(nameof(Conditions));
        }
        // ==========================================================
        // SHARED CATEGORY MANAGEMENT
        // ==========================================================
        [HttpPost]
        public IActionResult AddCategory(
            string name,
            string type)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                bool exists =
                    _context.Categories
                        .Any(c =>
                            c.Name == name &&
                            c.Type == type);
                if (!exists)
                {
                    var category = new Category
                    {
                        Name = name,
                        Type = type,
                        IsActive = true
                    };
                    _context.Categories.Add(category);
                    AddAuditLog(
                        "Created",
                        "Categories",
                        $"Added {type} category '{name}'.");
                    _context.SaveChanges();
                    TempData["Success"] =
                        "Category added.";
                }
                else
                {
                    TempData["Error"] =
                        "That category already exists.";
                }
            }
            return RedirectByCategoryType(type);
        }
        [HttpPost]
        public IActionResult EditCategory(
            int id,
            string name)
        {
            var category =
                _context.Categories.Find(id);
            var type =
                category?.Type ??
                "MedicalCondition";
            if (category != null &&
                !string.IsNullOrWhiteSpace(name))
            {
                var oldName =
                    category.Name;
                category.Name = name;
                AddAuditLog(
                    "Updated",
                    "Categories",
                    $"Updated {type} category '{oldName}' to '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Category updated.";
            }
            return RedirectByCategoryType(type);
        }
        [HttpPost]
        public IActionResult DeactivateCategory(int id)
        {
            var category =
                _context.Categories.Find(id);
            var type =
                category?.Type ??
                "MedicalCondition";
            if (category != null)
            {
                category.IsActive = false;
                AddAuditLog(
                    "Deactivated",
                    "Categories",
                    $"Archived {type} category '{category.Name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Category archived.";
            }
            return RedirectByCategoryType(type);
        }
        [HttpPost]
        public IActionResult ReactivateCategory(int id)
        {
            var category =
                _context.Categories.Find(id);
            var type =
                category?.Type ??
                "MedicalCondition";
            if (category != null)
            {
                category.IsActive = true;
                AddAuditLog(
                    "Reactivated",
                    "Categories",
                    $"Restored {type} category '{category.Name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Category restored.";
            }
            return RedirectByCategoryType(type);
        }
        private IActionResult RedirectByCategoryType(
            string type)
        {
            return type switch
            {
                "Allergy" =>
                    RedirectToAction(nameof(Allergies)),
                "Medication" =>
                    RedirectToAction(nameof(Medications)),
                _ =>
                    RedirectToAction(nameof(Conditions))
            };
        }
        // ==========================================================
        // ALLERGIES
        // ==========================================================
        public IActionResult Allergies()
        {
            SetSidebarData("Allergies");
            var vm = new AllergyListViewModel
            {
                PageTitle = "Allergies",
                Categories =
                    _context.Categories
                        .Where(c =>
                            c.Type == "Allergy" &&
                            c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList(),
                InactiveCategories =
                    _context.Categories
                        .Where(c =>
                            c.Type == "Allergy" &&
                            !c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList(),
                Allergies =
                    _context.Allergies
                        .Include(x => x.Category)
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.AllergyName)
                        .ToList(),
                InactiveAllergies =
                    _context.Allergies
                        .Include(x => x.Category)
                        .Where(x => !x.IsActive)
                        .OrderBy(x => x.AllergyName)
                        .ToList()
            };
            return View(vm);
        }
        [HttpPost]
        public IActionResult AddAllergy(
            string name,
            int categoryId,
            string? description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var allergy = new Allergy
                {
                    AllergyName = name,
                    CategoryId = categoryId,
                    Description = description,
                    IsActive = true
                };
                _context.Allergies.Add(allergy);
                AddAuditLog(
                    "Created",
                    "Allergies",
                    $"Added allergy '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Allergy added successfully.";
            }
            return RedirectToAction(nameof(Allergies));
        }
        [HttpPost]
        public IActionResult EditAllergy(
            int id,
            string name,
            int categoryId,
            string? description)
        {
            var allergy =
                _context.Allergies.Find(id);
            if (allergy != null)
            {
                var oldName =
                    allergy.AllergyName;
                allergy.AllergyName = name;
                allergy.CategoryId = categoryId;
                allergy.Description = description;
                AddAuditLog(
                    "Updated",
                    "Allergies",
                    $"Updated allergy '{oldName}' to '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Allergy updated successfully.";
            }
            return RedirectToAction(nameof(Allergies));
        }
        [HttpPost]
        public IActionResult ReactivateAllergy(int id)
        {
            var allergy =
                _context.Allergies.Find(id);
            if (allergy != null)
            {
                allergy.IsActive = true;
                AddAuditLog(
                    "Reactivated",
                    "Allergies",
                    $"Reactivated allergy '{allergy.AllergyName}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Allergy reactivated.";
            }
            return RedirectToAction(nameof(Allergies));
        }
        [HttpPost]
        public IActionResult DeleteAllergy(int id)
        {
            var allergy =
                _context.Allergies.Find(id);
            if (allergy != null)
            {
                allergy.IsActive = false;
                AddAuditLog(
                    "Deactivated",
                    "Allergies",
                    $"Deactivated allergy '{allergy.AllergyName}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Allergy deactivated.";
            }
            return RedirectToAction(nameof(Allergies));
        }
        // ==========================================================
        // MEDICATION CATEGORIES
        // ==========================================================
        private static readonly List<string>
            _defaultMedicationCategories = new()
            {
                "Antibiotics",
                "Pain Relief",
                "Chronic Condition"
            };
        // ==========================================================
        // MEDICATIONS
        // ==========================================================
        public IActionResult Medications()
        {
            SetSidebarData("Medications");
            var vm = new MedicationListViewModel
            {
                PageTitle = "Medications",
                Categories =
                    _context.Categories
                        .Where(c =>
                            c.Type == "Medication" &&
                            c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList(),
                InactiveCategories =
                    _context.Categories
                        .Where(c =>
                            c.Type == "Medication" &&
                            !c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList(),
                Medications =
                    _context.Medications
                        .Include(x => x.Category)
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.MedicationName)
                        .ToList(),
                InactiveMedications =
                    _context.Medications
                        .Include(x => x.Category)
                        .Where(x => !x.IsActive)
                        .OrderBy(x => x.MedicationName)
                        .ToList()
            };
            return View(vm);
        }
        [HttpPost]
        public IActionResult AddMedication(
            string name,
            int categoryId,
            string? description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var medication = new Medication
                {
                    MedicationName = name,
                    CategoryId = categoryId,
                    Description = description,
                    IsActive = true
                };
                _context.Medications.Add(medication);
                AddAuditLog(
                    "Created",
                    "Medications",
                    $"Added medication '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Medication added successfully.";
            }
            return RedirectToAction(nameof(Medications));
        }
        [HttpPost]
        public IActionResult EditMedication(
            int id,
            string name,
            int categoryId,
            string? description)
        {
            var medication =
                _context.Medications.Find(id);
            if (medication != null)
            {
                var oldName =
                    medication.MedicationName;
                medication.MedicationName = name;
                medication.CategoryId = categoryId;
                medication.Description = description;
                AddAuditLog(
                    "Updated",
                    "Medications",
                    $"Updated medication '{oldName}' to '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Medication updated successfully.";
            }
            return RedirectToAction(nameof(Medications));
        }
        [HttpPost]
        public IActionResult ReactivateMedication(int id)
        {
            var medication =
                _context.Medications.Find(id);
            if (medication != null)
            {
                medication.IsActive = true;
                AddAuditLog(
                    "Reactivated",
                    "Medications",
                    $"Reactivated medication '{medication.MedicationName}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Medication reactivated.";
            }
            return RedirectToAction(nameof(Medications));
        }
        [HttpPost]
        public IActionResult DeleteMedication(int id)
        {
            var medication =
                _context.Medications.Find(id);
            if (medication != null)
            {
                medication.IsActive = false;
                AddAuditLog(
                    "Deactivated",
                    "Medications",
                    $"Deactivated medication '{medication.MedicationName}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Medication deactivated.";
            }
            return RedirectToAction(nameof(Medications));
        }
        // ==========================================================
        // AUDIT LOG
        // ==========================================================
        public async Task<IActionResult> AuditLog()
        {
            SetSidebarData("AuditLog");
            var logs =
                await _context.AuditLogs
                    .OrderByDescending(
                        x => x.ActionDate)
                    .ToListAsync();
            var entries =
                new List<AuditEntry>();
            foreach (var log in logs)
            {
                string role = "Unspecified";
                if (!string.IsNullOrWhiteSpace(
                    log.UserName))
                {
                    var user =
                        await _context.Users
                            .FirstOrDefaultAsync(u =>
                                u.UserName ==
                                    log.UserName ||
                                u.Email ==
                                    log.UserName);
                    if (user != null)
                    {
                        var roles =
                            await _userManager
                                .GetRolesAsync(user);
                        if (roles.Count > 0)
                        {
                            role =
                                string.Join(
                                    ", ",
                                    roles);
                        }
                    }
                }
                entries.Add(
                    new AuditEntry
                    {
                        Timestamp =
                            log.ActionDate
                                .ToString("g"),
                        User =
                            string.IsNullOrWhiteSpace(
                                log.UserName)
                                ? "System"
                                : log.UserName,
                        Role = role,
                        Action =
                            log.Action,
                        Details =
                            $"{log.TableName} — {log.Details}"
                    });
            }
            var vm =
                new AuditLogViewModel
                {
                    Entries = entries
                };
            return View(vm);
        }
        // ==========================================================
        // SYSTEM TABLES
        // ==========================================================
        public IActionResult SystemTables()
        {
            SetSidebarData("SystemTables");
            var vm =
                new SystemTablesViewModel
                {
                    SampleTypes =
                        _context.SampleTypeLookups
                            .Where(x => x.IsActive)
                            .OrderBy(x => x.Name)
                            .ToList(),
                    InactiveSampleTypes =
                        _context.SampleTypeLookups
                            .Where(x => !x.IsActive)
                            .OrderBy(x => x.Name)
                            .ToList(),
                    Units =
                        _context.Units
                            .Where(x => x.IsActive)
                            .OrderBy(x => x.Name)
                            .ToList(),
                    InactiveUnits =
                        _context.Units
                            .Where(x => !x.IsActive)
                            .OrderBy(x => x.Name)
                            .ToList()
                };
            return View(vm);
        }
        // ==========================================================
        // SAMPLE TYPES
        // ==========================================================
        [HttpPost]
        public IActionResult AddSampleType(
            string name,
            string? description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.SampleTypeLookups.Add(
                    new SampleTypeLookup
                    {
                        Name = name,
                        Description = description,
                        IsActive = true
                    });
                AddAuditLog(
                    "Created",
                    "Sample Types",
                    $"Added sample type '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Sample type added.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        [HttpPost]
        public IActionResult EditSampleType(
            int id,
            string name,
            string? description)
        {
            var item =
                _context.SampleTypeLookups.Find(id);
            if (item != null &&
                !string.IsNullOrWhiteSpace(name))
            {
                var oldName =
                    item.Name;
                item.Name = name;
                item.Description = description;
                AddAuditLog(
                    "Updated",
                    "Sample Types",
                    $"Updated sample type '{oldName}' to '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Sample type updated.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        [HttpPost]
        public IActionResult DeleteSampleType(int id)
        {
            var item =
                _context.SampleTypeLookups.Find(id);
            if (item != null)
            {
                var name =
                    item.Name;
                _context.SampleTypeLookups.Remove(item);
                AddAuditLog(
                    "Deleted",
                    "Sample Types",
                    $"Deleted sample type '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Sample type deleted.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        [HttpPost]
        public IActionResult DeactivateSampleType(int id)
        {
            var item =
                _context.SampleTypeLookups.Find(id);
            if (item != null)
            {
                item.IsActive = false;
                AddAuditLog(
                    "Deactivated",
                    "Sample Types",
                    $"Deactivated sample type '{item.Name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Sample type deactivated.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        [HttpPost]
        public IActionResult ReactivateSampleType(int id)
        {
            var item =
                _context.SampleTypeLookups.Find(id);
            if (item != null)
            {
                item.IsActive = true;
                AddAuditLog(
                    "Reactivated",
                    "Sample Types",
                    $"Reactivated sample type '{item.Name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Sample type reactivated.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        // ==========================================================
        // UNITS
        // ==========================================================
        [HttpPost]
        public IActionResult AddUnit(
            string name,
            string? description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _context.Units.Add(
                    new Unit
                    {
                        Name = name,
                        Description = description,
                        IsActive = true
                    });
                AddAuditLog(
                    "Created",
                    "Units",
                    $"Added unit '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Unit added.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        [HttpPost]
        public IActionResult DeleteUnit(int id)
        {
            var item =
                _context.Units.Find(id);
            if (item != null)
            {
                var name =
                    item.Name;
                _context.Units.Remove(item);
                AddAuditLog(
                    "Deleted",
                    "Units",
                    $"Deleted unit '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Unit deleted.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        [HttpPost]
        public IActionResult EditUnit(
            int id,
            string name,
            string? description)
        {
            var item =
                _context.Units.Find(id);
            if (item != null &&
                !string.IsNullOrWhiteSpace(name))
            {
                var oldName =
                    item.Name;
                item.Name = name;
                item.Description = description;
                AddAuditLog(
                    "Updated",
                    "Units",
                    $"Updated unit '{oldName}' to '{name}'.");
                _context.SaveChanges();
                TempData["Success"] =
                    "Unit updated.";
            }
            return RedirectToAction(nameof(SystemTables));
        }
        // ==========================================================
        // ADMIN PROFILE
        // ==========================================================
        // GET: /Admin/AdminProfile
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            var model = new AdminProfileViewModel
            {
                Id = user.Id,
                Name = user.FirstName,
                Surname = user.LastName,
                IDNumber = user.SouthAfricanID,
                Role = roles.FirstOrDefault() ?? "Admin",
                Email = user.Email,
                Cellphone = user.PhoneNumb,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2
            };
            return View(model);
        }
        // POST: /Admin/AdminProfile
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminProfile(AdminProfileViewModel model)
        {
            // Role and IDNumber are locked fields - never bound from the form,
            // so don't validate them even if ModelState complains.
            ModelState.Remove(nameof(AdminProfileViewModel.IDNumber));
            ModelState.Remove(nameof(AdminProfileViewModel.Role));
            if (!ModelState.IsValid)
                return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            // Editable fields only.
            user.FirstName = model.Name;
            user.LastName = model.Surname;
            user.PhoneNumb = model.Cellphone;
            user.AddressLine1 = model.AddressLine1;
            user.AddressLine2 = model.AddressLine2;
            // Email/username changes need Identity's own flow so the
            // security stamp and normalized username stay in sync.
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!emailResult.Succeeded)
                {
                    foreach (var err in emailResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }
                await _userManager.SetUserNameAsync(user, model.Email);
            }
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }
            // Keep the auth cookie in sync with the (possibly changed) security stamp.
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(AdminProfile));
        }
    }
}