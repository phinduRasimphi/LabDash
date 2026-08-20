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

            // Search by test name or sample type
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(t =>
                    t.Name.Contains(searchString) ||
                    t.RequiredSampleType.Contains(searchString));
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

        public IActionResult Create()
        {
            LoadCategories();
            LoadConsumables();

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
            // Navigation properties are loaded by EF
            // so they should not be validated from the form.
            ModelState.Remove("TestCategory");
            ModelState.Remove("TestRequestItems");
            ModelState.Remove("TechnicianTestTypes");
            ModelState.Remove("TestTypeConsumables");

            // --------------------------------------------------------
            // CHECK DUPLICATE TEST NAME
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(testType.Name))
            {
                bool nameExists = await _context.TestTypes
                    .AnyAsync(t =>
                        t.Name.ToLower() ==
                        testType.Name.ToLower());

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
                if (testType.ReferenceRangeLow >
                    testType.ReferenceRangeHigh)
                {
                    ModelState.AddModelError(
                        "ReferenceRangeHigh",
                        "The maximum reference range cannot be lower than the minimum.");
                }
            }


            // --------------------------------------------------------
            // CHECK CONSUMABLES
            // --------------------------------------------------------

            if (SelectedConsumableIds != null)
            {
                if (ConsumableQuantities == null ||
                    SelectedConsumableIds.Length !=
                    ConsumableQuantities.Length)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity for every selected consumable.");
                }
                else
                {
                    foreach (var quantity in ConsumableQuantities)
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


            // --------------------------------------------------------
            // SAVE
            // --------------------------------------------------------

            if (ModelState.IsValid)
            {
                testType.TestTypeConsumables =
                    new List<TestTypeConsumable>();

                if (SelectedConsumableIds != null &&
                    ConsumableQuantities != null)
                {
                    for (int i = 0;
                         i < SelectedConsumableIds.Length;
                         i++)
                    {
                        testType.TestTypeConsumables.Add(
                            new TestTypeConsumable
                            {
                                ConsumableId =
                                    SelectedConsumableIds[i],

                                QuantityRequired =
                                    ConsumableQuantities[i]
                            });
                    }
                }

                _context.TestTypes.Add(testType);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Test type created successfully.";

                return RedirectToAction(nameof(Index));
            }


            // If validation fails, reload dropdowns
            LoadCategories(testType.TestCategoryId);
            LoadConsumables();

            return View(testType);
        }


        // ============================================================
        // EDIT - GET
        // ============================================================

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

            LoadCategories(testType.TestCategoryId);
            LoadConsumables();

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
            if (id != testType.Id)
            {
                return NotFound();
            }

            // Remove navigation properties from validation
            ModelState.Remove("TestCategory");
            ModelState.Remove("TestRequestItems");
            ModelState.Remove("TechnicianTestTypes");
            ModelState.Remove("TestTypeConsumables");


            // --------------------------------------------------------
            // CHECK DUPLICATE NAME
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(testType.Name))
            {
                bool nameExists = await _context.TestTypes
                    .AnyAsync(t =>
                        t.Id != id &&
                        t.Name.ToLower() ==
                        testType.Name.ToLower());

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
                if (testType.ReferenceRangeLow >
                    testType.ReferenceRangeHigh)
                {
                    ModelState.AddModelError(
                        "ReferenceRangeHigh",
                        "The maximum reference range cannot be lower than the minimum.");
                }
            }


            // --------------------------------------------------------
            // CHECK CONSUMABLES
            // --------------------------------------------------------

            if (SelectedConsumableIds != null)
            {
                if (ConsumableQuantities == null ||
                    SelectedConsumableIds.Length !=
                    ConsumableQuantities.Length)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity for every selected consumable.");
                }
                else
                {
                    foreach (var quantity in ConsumableQuantities)
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


            // --------------------------------------------------------
            // UPDATE
            // --------------------------------------------------------

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


                // Update TestType fields

                existingTestType.Name =
                    testType.Name;

                existingTestType.TestCategoryId =
                    testType.TestCategoryId;

                existingTestType.RequiredSampleType =
                    testType.RequiredSampleType;

                existingTestType.UnitOfMeasurement =
                    testType.UnitOfMeasurement;

                existingTestType.TurnaroundTimeHours =
                    testType.TurnaroundTimeHours;

                existingTestType.ReferenceRangeLow =
                    testType.ReferenceRangeLow;

                existingTestType.ReferenceRangeHigh =
                    testType.ReferenceRangeHigh;


                // ----------------------------------------------------
                // REMOVE OLD CONSUMABLE RELATIONSHIPS
                // ----------------------------------------------------

                _context.TestTypeConsumables.RemoveRange(
                    existingTestType.TestTypeConsumables);


                // ----------------------------------------------------
                // ADD UPDATED CONSUMABLE RELATIONSHIPS
                // ----------------------------------------------------

                if (SelectedConsumableIds != null &&
                    ConsumableQuantities != null)
                {
                    for (int i = 0;
                         i < SelectedConsumableIds.Length;
                         i++)
                    {
                        _context.TestTypeConsumables.Add(
                            new TestTypeConsumable
                            {
                                TestTypeId = id,

                                ConsumableId =
                                    SelectedConsumableIds[i],

                                QuantityRequired =
                                    ConsumableQuantities[i]
                            });
                    }
                }


                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Test type updated successfully.";

                return RedirectToAction(nameof(Index));
            }


            LoadCategories(testType.TestCategoryId);
            LoadConsumables();

            return View(testType);
        }


        // ============================================================
        // DELETE - GET
        // ============================================================

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
            // DO NOT DELETE TEST TYPES ALREADY USED IN REQUESTS
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
            // REMOVE TECHNICIAN ASSIGNMENTS
            // --------------------------------------------------------

            if (testType.TechnicianTestTypes != null &&
                testType.TechnicianTestTypes.Any())
            {
                _context.TechnicianTestTypes.RemoveRange(
                    testType.TechnicianTestTypes);
            }


            // --------------------------------------------------------
            // REMOVE CONSUMABLE RELATIONSHIPS
            // --------------------------------------------------------

            if (testType.TestTypeConsumables != null &&
                testType.TestTypeConsumables.Any())
            {
                _context.TestTypeConsumables.RemoveRange(
                    testType.TestTypeConsumables);
            }


            // --------------------------------------------------------
            // DELETE TEST TYPE
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
    }
}