using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class TestTypesController : Controller
    {
        private readonly LabDbContext _context;

        public TestTypesController(LabDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // INDEX
        // ============================================================

        public async Task<IActionResult> Index(string? searchString)
        {
            var query = _context.TestTypes
                .Include(t => t.TestCategory)
                .Include(t => t.TestTypeConsumables)
                    .ThenInclude(tc => tc.Consumable)
                .Include(t => t.TechnicianTestTypes)
                    .ThenInclude(tt => tt.Technician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(t =>
                    t.Name.Contains(searchString) ||
                    (t.RequiredSampleType != null &&
                     t.RequiredSampleType.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            var testTypes = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(testTypes);
        }


        // ============================================================
        // DETAILS
        // ============================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testType = await _context.TestTypes
                .Include(t => t.TestCategory)
                .Include(t => t.TestTypeConsumables)
                    .ThenInclude(tc => tc.Consumable)
                .Include(t => t.TechnicianTestTypes)
                    .ThenInclude(tt => tt.Technician)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testType == null)
            {
                return NotFound();
            }

            return View(testType);
        }


        // ============================================================
        // CREATE - GET
        // ============================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadCategories();
            LoadConsumables();
            LoadSampleTypes();

            return View();
        }


        // ============================================================
        // CREATE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TestType testType,
            int[]? SelectedConsumableIds,
            int[]? ConsumableQuantities)
        {
            // --------------------------------------------------------
            // REMOVE NAVIGATION PROPERTY VALIDATION
            // --------------------------------------------------------

            ModelState.Remove("TestCategory");
            ModelState.Remove("TestRequestItems");
            ModelState.Remove("TechnicianTestTypes");
            ModelState.Remove("TestTypeConsumables");


            // --------------------------------------------------------
            // TEST NAME REQUIRED
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(testType.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Please enter a test name.");
            }


            // --------------------------------------------------------
            // CHECK DUPLICATE TEST NAME
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(testType.Name))
            {
                string testName = testType.Name.Trim();

                bool nameExists = await _context.TestTypes
                    .AnyAsync(t =>
                        t.Name.ToLower() == testName.ToLower());

                if (nameExists)
                {
                    ModelState.AddModelError(
                        "Name",
                        "A test type with this name already exists.");
                }
            }


            // --------------------------------------------------------
            // CHECK REFERENCE RANGE
            // --------------------------------------------------------

            if (testType.ReferenceRangeLow.HasValue &&
                testType.ReferenceRangeHigh.HasValue)
            {
                if (testType.ReferenceRangeLow.Value >
                    testType.ReferenceRangeHigh.Value)
                {
                    ModelState.AddModelError(
                        "ReferenceRangeHigh",
                        "The maximum reference range cannot be lower than the minimum.");
                }
            }


            // --------------------------------------------------------
            // CHECK CONSUMABLES
            // --------------------------------------------------------

            if (SelectedConsumableIds != null &&
                SelectedConsumableIds.Length > 0)
            {
                if (ConsumableQuantities == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity for every selected consumable.");
                }
                else if (
                    SelectedConsumableIds.Length !=
                    ConsumableQuantities.Length)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity for every selected consumable.");
                }
                else
                {
                    foreach (int quantity in ConsumableQuantities)
                    {
                        if (quantity <= 0)
                        {
                            ModelState.AddModelError(
                                "",
                                "Consumable quantities must be greater than zero.");

                            break;
                        }
                    }
                }
            }


            // ========================================================
            // SAVE
            // ========================================================

            if (ModelState.IsValid)
            {
                // ----------------------------------------------------
                // Clean the test name
                // ----------------------------------------------------

                testType.Name = testType.Name.Trim();


                // ----------------------------------------------------
                // Save TestType FIRST
                //
                // This allows EF/database to generate testType.Id
                // before we create TestTypeConsumable records.
                // ----------------------------------------------------

                _context.TestTypes.Add(testType);

                await _context.SaveChangesAsync();


                // ----------------------------------------------------
                // Save selected consumables
                // ----------------------------------------------------

                if (SelectedConsumableIds != null &&
                    ConsumableQuantities != null &&
                    SelectedConsumableIds.Length > 0)
                {
                    for (int i = 0;
                         i < SelectedConsumableIds.Length;
                         i++)
                    {
                        var testTypeConsumable =
                            new TestTypeConsumable
                            {
                                TestTypeId = testType.Id,

                                ConsumableId =
                                    SelectedConsumableIds[i],

                                QuantityRequired =
                                    ConsumableQuantities[i]
                            };

                        _context.TestTypeConsumables.Add(
                            testTypeConsumable);
                    }

                    await _context.SaveChangesAsync();
                }


                // ----------------------------------------------------
                // Success
                // ----------------------------------------------------

                TempData["SuccessMessage"] =
                    "Test type created successfully.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // VALIDATION FAILED
            // ========================================================

            LoadCategories(testType.TestCategoryId);

            LoadConsumables();

            LoadSampleTypes(testType.RequiredSampleType);

            return View(testType);
        }


        // ============================================================
        // EDIT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testType = await _context.TestTypes
                .Include(t => t.TestTypeConsumables)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testType == null)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // Load dropdowns
            // --------------------------------------------------------

            LoadCategories(testType.TestCategoryId);

            LoadConsumables();

            LoadSampleTypes(testType.RequiredSampleType);


            return View(testType);
        }


        // ============================================================
        // EDIT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            TestType testType,
            int[]? SelectedConsumableIds,
            int[]? ConsumableQuantities)
        {
            // --------------------------------------------------------
            // Check ID
            // --------------------------------------------------------

            if (id != testType.Id)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // REMOVE NAVIGATION PROPERTY VALIDATION
            // --------------------------------------------------------

            ModelState.Remove("TestCategory");
            ModelState.Remove("TestRequestItems");
            ModelState.Remove("TechnicianTestTypes");
            ModelState.Remove("TestTypeConsumables");


            // --------------------------------------------------------
            // TEST NAME REQUIRED
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(testType.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Please enter a test name.");
            }


            // --------------------------------------------------------
            // CHECK DUPLICATE NAME
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(testType.Name))
            {
                string testName = testType.Name.Trim();

                bool nameExists = await _context.TestTypes
                    .AnyAsync(t =>
                        t.Id != id &&
                        t.Name.ToLower() == testName.ToLower());

                if (nameExists)
                {
                    ModelState.AddModelError(
                        "Name",
                        "A test type with this name already exists.");
                }
            }


            // --------------------------------------------------------
            // CHECK REFERENCE RANGE
            // --------------------------------------------------------

            if (testType.ReferenceRangeLow.HasValue &&
                testType.ReferenceRangeHigh.HasValue)
            {
                if (testType.ReferenceRangeLow.Value >
                    testType.ReferenceRangeHigh.Value)
                {
                    ModelState.AddModelError(
                        "ReferenceRangeHigh",
                        "The maximum reference range cannot be lower than the minimum.");
                }
            }


            // --------------------------------------------------------
            // CHECK CONSUMABLES
            // --------------------------------------------------------

            if (SelectedConsumableIds != null &&
                SelectedConsumableIds.Length > 0)
            {
                if (ConsumableQuantities == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity for every selected consumable.");
                }
                else if (
                    SelectedConsumableIds.Length !=
                    ConsumableQuantities.Length)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity for every selected consumable.");
                }
                else
                {
                    foreach (int quantity in ConsumableQuantities)
                    {
                        if (quantity <= 0)
                        {
                            ModelState.AddModelError(
                                "",
                                "Consumable quantities must be greater than zero.");

                            break;
                        }
                    }
                }
            }


            // ========================================================
            // UPDATE
            // ========================================================

            if (ModelState.IsValid)
            {
                var existingTestType =
                    await _context.TestTypes
                        .Include(t => t.TestTypeConsumables)
                        .FirstOrDefaultAsync(t => t.Id == id);

                if (existingTestType == null)
                {
                    return NotFound();
                }


                // ----------------------------------------------------
                // Update basic fields
                // ----------------------------------------------------

                existingTestType.Name =
                    testType.Name.Trim();

                existingTestType.TestCategoryId =
                    testType.TestCategoryId;

                existingTestType.RequiredSampleType =
                    testType.RequiredSampleType;

                existingTestType.UnitOfMeasurement =
                    testType.UnitOfMeasurement;

                existingTestType.ReferenceRangeLow =
                    testType.ReferenceRangeLow;

                existingTestType.ReferenceRangeHigh =
                    testType.ReferenceRangeHigh;

                existingTestType.TurnaroundTimeHours =
                    testType.TurnaroundTimeHours;


                // ----------------------------------------------------
                // Remove old consumables
                // ----------------------------------------------------

                if (existingTestType.TestTypeConsumables != null &&
                    existingTestType.TestTypeConsumables.Any())
                {
                    _context.TestTypeConsumables.RemoveRange(
                        existingTestType.TestTypeConsumables);
                }


                // ----------------------------------------------------
                // Add new consumables
                // ----------------------------------------------------

                if (SelectedConsumableIds != null &&
                    ConsumableQuantities != null &&
                    SelectedConsumableIds.Length > 0)
                {
                    for (int i = 0;
                         i < SelectedConsumableIds.Length;
                         i++)
                    {
                        var testTypeConsumable =
                            new TestTypeConsumable
                            {
                                TestTypeId = id,

                                ConsumableId =
                                    SelectedConsumableIds[i],

                                QuantityRequired =
                                    ConsumableQuantities[i]
                            };

                        _context.TestTypeConsumables.Add(
                            testTypeConsumable);
                    }
                }


                // ----------------------------------------------------
                // Save changes
                // ----------------------------------------------------

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    "Test type updated successfully.";

                return RedirectToAction(nameof(Index));
            }


            // ========================================================
            // VALIDATION FAILED
            // ========================================================

            LoadCategories(testType.TestCategoryId);

            LoadConsumables();

            LoadSampleTypes(testType.RequiredSampleType);

            return View(testType);
        }


        // ============================================================
        // DELETE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testType = await _context.TestTypes
                .Include(t => t.TestCategory)
                .Include(t => t.TestTypeConsumables)
                    .ThenInclude(tc => tc.Consumable)
                .Include(t => t.TechnicianTestTypes)
                    .ThenInclude(tt => tt.Technician)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testType == null)
            {
                return NotFound();
            }

            return View(testType);
        }


        // ============================================================
        // DELETE - POST
        // ============================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testType = await _context.TestTypes
                .Include(t => t.TestTypeConsumables)
                .Include(t => t.TechnicianTestTypes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testType == null)
            {
                return NotFound();
            }


            // --------------------------------------------------------
            // Check if used in Test Requests
            // --------------------------------------------------------

            bool usedInRequests =
                await _context.TestRequestItems
                    .AnyAsync(x => x.TestTypeId == id);

            if (usedInRequests)
            {
                TempData["ErrorMessage"] =
                    "This test type cannot be deleted because it has already been used in a test request.";

                return RedirectToAction(nameof(Index));
            }


            // --------------------------------------------------------
            // Remove technician assignments
            // --------------------------------------------------------

            if (testType.TechnicianTestTypes != null &&
                testType.TechnicianTestTypes.Any())
            {
                _context.TechnicianTestTypes.RemoveRange(
                    testType.TechnicianTestTypes);
            }


            // --------------------------------------------------------
            // Remove consumable relationships
            // --------------------------------------------------------

            if (testType.TestTypeConsumables != null &&
                testType.TestTypeConsumables.Any())
            {
                _context.TestTypeConsumables.RemoveRange(
                    testType.TestTypeConsumables);
            }


            // --------------------------------------------------------
            // Delete test type
            // --------------------------------------------------------

            _context.TestTypes.Remove(testType);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Test type deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // CHECK TEST TYPE EXISTS
        // ============================================================

        private bool TestTypeExists(int id)
        {
            return _context.TestTypes
                .Any(t => t.Id == id);
        }


        // ============================================================
        // LOAD TEST CATEGORIES
        // ============================================================

        private void LoadCategories(int? selectedCategory = null)
        {
            ViewData["TestCategoryId"] =
                new SelectList(
                    _context.TestCategories
                        .OrderBy(c => c.CategoryName)
                        .ToList(),
                    "TestCategoryId",
                    "CategoryName",
                    selectedCategory);
        }


        // ============================================================
        // LOAD CONSUMABLES
        // ============================================================

        private void LoadConsumables()
        {
            ViewBag.Consumables =
                _context.Consumables
                    .OrderBy(c => c.Name)
                    .ToList();
        }


        // ============================================================
        // LOAD SAMPLE TYPES
        // ============================================================

        private void LoadSampleTypes(
            string? selectedSampleType = null)
        {
            ViewData["SampleTypes"] =
                new SelectList(
                    _context.SampleTypeLookups
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Name)
                        .ToList(),
                    "Name",
                    "Name",
                    selectedSampleType);
        }
    }
}