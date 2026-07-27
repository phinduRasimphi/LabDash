using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class AdminController : Controller
    {
        private readonly LabDbContext _context;

        public AdminController(LabDbContext context)
        {
            _context = context;
        }

        // Categories (we'll move these into the database later)
        private static readonly List<string> _condCategories = new()
        {
            "Cardiovascular",
            "Metabolic",
            "Respiratory",
            "Renal"
        };

        private static readonly List<string> _allergyCategories = new()
        {
            "Drug Allergies",
            "Food Allergies",
            "Environmental"
        };

        private static readonly List<string> _medCategories = new()
        {
            "Antibiotic",
            "Analgesic",
            "Antidiabetic",
            "Antihypertensive"
        };

        private void SetSidebarData(string activePage)
        {
            ViewData["ActivePage"] = activePage;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // ===========================
        // Dashboard
        // ===========================

        public IActionResult Dashboard()
        {
            SetSidebarData("Dashboard");

            var vm = new AdminDashboardViewModel
            {
                ConditionCount = _context.MedicalConditions.Count(x => x.IsActive),
                AllergyCount = _context.Allergies.Count(x => x.IsActive),
                MedicationCount = _context.Medications.Count(x => x.IsActive),

                RecentConditions = _context.MedicalConditions
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
                Categories = _condCategories,

                Conditions = _context.MedicalConditions
                    .Where(x => x.IsActive)
                    .ToList(),

                InactiveConditions = _context.MedicalConditions
                    .Where(x => !x.IsActive)
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult AddCondition(string name, string category)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var condition = new MedicalCondition
                {
                    ConditionName = name,
                    Category = category,
                    IsActive = true
                };

                _context.MedicalConditions.Add(condition);
                _context.SaveChanges();

                TempData["Success"] = "Condition added successfully.";
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

        // ===========================
        // ALLERGIES
        // ===========================

        public IActionResult Allergies()
        {
            SetSidebarData("Allergies");

            var vm = new AllergyListViewModel
            {
                PageTitle = "Allergies",
                Categories = _allergyCategories,

                Allergies = _context.Allergies
                    .Where(x => x.IsActive)
                    .ToList(),

                InactiveAllergies = _context.Allergies
                    .Where(x => !x.IsActive)
                    .ToList()
            };

            return View(vm);
        }

        // ===========================
        // MEDICATIONS
        // ===========================

        public IActionResult Medications()
        {
            SetSidebarData("Medications");

            var vm = new MedicationListViewModel
            {
                PageTitle = "Medications",
                Categories = _medCategories,

                Medications = _context.Medications
                    .Where(x => x.IsActive)
                    .ToList(),

                InactiveMedications = _context.Medications
                    .Where(x => !x.IsActive)
                    .ToList()
            };

            return View(vm);
        }

        // ===========================
        // SYSTEM TABLES
        // ===========================

        public IActionResult SystemTables()
        {
            SetSidebarData("SystemTables");

            return View();
        }

        // ===========================
        // AUDIT LOG
        // ===========================

        public IActionResult AuditLog()
        {
            SetSidebarData("AuditLog");

            return View();
        }

        // ===========================
        // LOGOUT
        // ===========================

        [HttpPost]
        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}