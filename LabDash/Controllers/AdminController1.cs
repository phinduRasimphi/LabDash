//using LabDash.Areas.Identity.Data;
//using LabDash.Models;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;

//namespace LabDash.Controllers
//{
//    public class AdminController : Controller
//    {
//        // ── PROTOTYPE STATIC DATA ──────────────────────────
//        private static List<string> _condCategories = new() { "Cardiovascular", "Metabolic", "Respiratory", "Renal" };
//        private static List<string> _allergyCategories = new() { "Drug Allergies", "Food Allergies", "Environmental" };
//        private static List<string> _medCategories = new() { "Antidiabetic", "Antihypertensive", "Anticoagulant", "Statin", "Antibiotic", "Analgesic" };

//        private static List<MedicalItem> _conditions = new()
//        {
//            new() { Id=1, Name="Hypertension",       Category="Cardiovascular", Patients=12, DateAdded="8 May 2026"  },
//            new() { Id=2, Name="Type 2 Diabetes",    Category="Metabolic",      Patients=9,  DateAdded="10 May 2026" },
//            new() { Id=3, Name="Asthma",             Category="Respiratory",    Patients=7,  DateAdded="5 May 2026"  },
//            new() { Id=4, Name="CKD Stage 3",        Category="Renal",          Patients=4,  DateAdded="2 May 2026"  },
//            new() { Id=5, Name="Atrial Fibrillation",Category="Cardiovascular", Patients=3,  DateAdded="28 Apr 2026" },
//            new() { Id=6, Name="Hypothyroidism",     Category="Metabolic",      Patients=6,  DateAdded="25 Apr 2026" },
//            new() { Id=7, Name="COPD",               Category="Respiratory",    Patients=2,  DateAdded="20 Apr 2026" },
//        };

//        private static List<MedicalItem> _allergies = new()
//        {
//            new() { Id=1, Name="Penicillin",    Category="Drug Allergies",  Patients=8, DateAdded="12 May 2026" },
//            new() { Id=2, Name="Sulfonamides",  Category="Drug Allergies",  Patients=5, DateAdded="9 May 2026"  },
//            new() { Id=3, Name="NSAIDs",        Category="Drug Allergies",  Patients=6, DateAdded="7 May 2026"  },
//            new() { Id=4, Name="Peanuts",       Category="Food Allergies",  Patients=3, DateAdded="3 May 2026"  },
//            new() { Id=5, Name="Shellfish",     Category="Food Allergies",  Patients=2, DateAdded="1 May 2026"  },
//            new() { Id=6, Name="Pollen",        Category="Environmental",   Patients=4, DateAdded="28 Apr 2026" },
//            new() { Id=7, Name="Latex",         Category="Environmental",   Patients=1, DateAdded="25 Apr 2026" },
//        };

//        private static List<MedicalItem> _medications = new()
//        {
//            new() { Id=1, Name="Metformin 500mg",    Category="Antidiabetic",     Patients=9,  DateAdded="10 May 2026" },
//            new() { Id=2, Name="Atorvastatin 20mg",  Category="Statin",           Patients=7,  DateAdded="9 May 2026"  },
//            new() { Id=3, Name="Amlodipine 5mg",     Category="Antihypertensive", Patients=6,  DateAdded="7 May 2026"  },
//            new() { Id=4, Name="Warfarin 5mg",       Category="Anticoagulant",    Patients=3,  DateAdded="4 May 2026"  },
//            new() { Id=5, Name="Amoxicillin 500mg",  Category="Antibiotic",       Patients=4,  DateAdded="2 May 2026"  },
//            new() { Id=6, Name="Paracetamol 500mg",  Category="Analgesic",        Patients=11, DateAdded="30 Apr 2026" },
//            new() { Id=7, Name="Lisinopril 10mg",    Category="Antihypertensive", Patients=5,  DateAdded="28 Apr 2026" },
//        };

//        private static List<SystemTableItem> _sampleTypes = new()
//        {
//            new() { Id=1, Name="Whole Blood (EDTA)" },
//            new() { Id=2, Name="Plasma"             },
//            new() { Id=3, Name="Serum"              },
//            new() { Id=4, Name="Bone Marrow"        },
//            new() { Id=5, Name="Urine"              },
//            new() { Id=6, Name="CSF"                },
//        };

//        private static List<SystemTableItem> _units = new()
//        {
//            new() { Id=1, Name="g/dL",     Description="Grams per decilitre"         },
//            new() { Id=2, Name="x10³/µL",  Description="Thousands per microlitre"    },
//            new() { Id=3, Name="µL",       Description="Microlitre"                  },
//            new() { Id=4, Name="g/L",      Description="Grams per litre"             },
//            new() { Id=5, Name="%",        Description="Percentage"                  },
//            new() { Id=6, Name="mmol/L",   Description="Millimoles per litre"        },
//            new() { Id=7, Name="fL",       Description="Femtolitre"                  },
//        };

//        private static List<AuditEntry> _auditLog = new()
//        {
//            new() { Timestamp="2026-05-15 08:42", User="Admin",          Role="Admin",      Action="Create", Details="Added condition: CKD Stage 3 (Renal)"                        },
//            new() { Timestamp="2026-05-15 08:30", User="Dr Makuwa",      Role="Doctor",     Action="Submit", Details="Test request REQ-1041 submitted for patient Thabo Dlamini"   },
//            new() { Timestamp="2026-05-15 07:59", User="Tech Naidoo",    Role="Technician", Action="Update", Details="Result captured for REQ-1041 (Full Blood Count)"             },
//            new() { Timestamp="2026-05-14 16:10", User="Thabo Dlamini",  Role="Patient",    Action="Update", Details="Medical history updated: added Metformin 500mg"              },
//            new() { Timestamp="2026-05-14 14:33", User="Admin",          Role="Admin",      Action="Create", Details="Added medication: Warfarin 5mg (Anticoagulant)"              },
//            new() { Timestamp="2026-05-14 09:15", User="Tech Naidoo",    Role="Technician", Action="Verify", Details="Result verified for REQ-0987 (Coagulation Panel)"            },
//            new() { Timestamp="2026-05-13 11:08", User="Dr Mokoena",     Role="Doctor",     Action="Release",Details="Results released to patient for REQ-0921"                   },
//            new() { Timestamp="2026-05-13 09:44", User="Admin",          Role="Admin",      Action="Delete", Details="Removed obsolete allergy category: Latex"                   },
//        };

//        // ── HELPER ────────────────────────────────────────
//        private void SetSidebarData(string activePage)
//        {
//            ViewData["ActivePage"] = activePage;
//        }

//        // ── 1. DASHBOARD ──────────────────────────────────
//        public IActionResult Index() => RedirectToAction("Dashboard");

//        public IActionResult Dashboard()
//        {
//            SetSidebarData("Dashboard");
//            ViewData["Title"] = "Dashboard";
//            var vm = new AdminDashboardViewModel
//            {
//                ConditionCount = _conditions.Count,
//                AllergyCount = _allergies.Count,
//                MedicationCount = _medications.Count,
//                UserCount = 73,
//                RecentConditions = _conditions.OrderByDescending(c => c.Id).Take(4).ToList(),
//                RecentMedications = _medications.OrderByDescending(m => m.Id).Take(4).ToList()
//            };
//            return View(vm);
//        }

//        // ── 2. CONDITIONS ─────────────────────────────────
//        public IActionResult Conditions()
//        {
//            SetSidebarData("Conditions");
//            ViewData["Title"] = "Conditions";
//            var vm = new AdminListViewModel { PageTitle = "Conditions", Categories = _condCategories, Items = _conditions };
//            return View(vm);
//        }

//        [HttpPost]
//        public IActionResult AddCondition(string name, string category)
//        {
//            if (!string.IsNullOrWhiteSpace(name))
//            {
//                _conditions.Add(new MedicalItem { Id = _conditions.Count + 1, Name = name, Category = category, Patients = 0, DateAdded = DateTime.Now.ToString("d MMM yyyy") });
//                TempData["Success"] = "Condition added.";
//            }
//            return RedirectToAction("Conditions");
//        }

//        [HttpPost]
//        public IActionResult DeleteCondition(int id)
//        {
//            _conditions.RemoveAll(c => c.Id == id);
//            TempData["Success"] = "Condition deleted.";
//            return RedirectToAction("Conditions");
//        }

//        // ── 3. ALLERGIES ──────────────────────────────────
//        public IActionResult Allergies()
//        {
//            SetSidebarData("Allergies");
//            ViewData["Title"] = "Allergies";
//            var vm = new AdminListViewModel { PageTitle = "Allergies", Categories = _allergyCategories, Items = _allergies };
//            return View(vm);
//        }

//        [HttpPost]
//        public IActionResult AddAllergy(string name, string category)
//        {
//            if (!string.IsNullOrWhiteSpace(name))
//            {
//                _allergies.Add(new MedicalItem { Id = _allergies.Count + 1, Name = name, Category = category, Patients = 0, DateAdded = DateTime.Now.ToString("d MMM yyyy") });
//                TempData["Success"] = "Allergy added.";
//            }
//            return RedirectToAction("Allergies");
//        }

//        [HttpPost]
//        public IActionResult DeleteAllergy(int id)
//        {
//            _allergies.RemoveAll(a => a.Id == id);
//            TempData["Success"] = "Allergy deleted.";
//            return RedirectToAction("Allergies");
//        }

//        // ── 4. MEDICATIONS ────────────────────────────────
//        public IActionResult Medications()
//        {
//            SetSidebarData("Medications");
//            ViewData["Title"] = "Medications";
//            var vm = new AdminListViewModel { PageTitle = "Medications", Categories = _medCategories, Items = _medications };
//            return View(vm);
//        }

//        [HttpPost]
//        public IActionResult AddMedication(string name, string category)
//        {
//            if (!string.IsNullOrWhiteSpace(name))
//            {
//                _medications.Add(new MedicalItem { Id = _medications.Count + 1, Name = name, Category = category, Patients = 0, DateAdded = DateTime.Now.ToString("d MMM yyyy") });
//                TempData["Success"] = "Medication added.";
//            }
//            return RedirectToAction("Medications");
//        }

//        [HttpPost]
//        public IActionResult DeleteMedication(int id)
//        {
//            _medications.RemoveAll(m => m.Id == id);
//            TempData["Success"] = "Medication deleted.";
//            return RedirectToAction("Medications");
//        }

//        // ── 5. SYSTEM TABLES ─────────────────────────────
//        public IActionResult SystemTables()
//        {
//            SetSidebarData("SystemTables");
//            ViewData["Title"] = "System Tables";
//            var vm = new SystemTablesViewModel { SampleTypes = _sampleTypes, Units = _units };
//            return View(vm);
//        }

//        // ── 6. AUDIT LOG ──────────────────────────────────
//        public IActionResult AuditLog()
//        {
//            SetSidebarData("AuditLog");
//            ViewData["Title"] = "Audit Log";
//            return View(new AuditLogViewModel { Entries = _auditLog });
//        }



//        [HttpPost]
//        public async Task<IActionResult> Logout()
//        {
//            HttpContext.Session.Clear();

//            await HttpContext.SignOutAsync();

//            return RedirectToAction("Landing", "Home");
//        }
//    }
//}