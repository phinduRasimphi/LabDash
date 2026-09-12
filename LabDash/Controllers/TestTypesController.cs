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

        public async Task<IActionResult> Index(string searchString)
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
                searchString = searchString.Trim();

                query = query.Where(t =>
                    t.Name.Contains(searchString) ||
                    t.Category.Contains(searchString) ||
                    t.RequiredSampleType.Contains(searchString));
            }

            var testTypes = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.SearchString = searchString;

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
        public async Task<IActionResult> Create()
        {
            await LoadCreateDropdowns();

            return View();
        }


        // ============================================================
        // CREATE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TestType model,
            List<int>? SelectedConsumableIds,
            Dictionary<int, int>? ConsumableQuantities)
        {
            // --------------------------------------------------------
            // Remove navigation properties from ModelState
            // --------------------------------------------------------

            ModelState.Remove("TestCategory");
            ModelState.Remove("TestRequestItems");
            ModelState.Remove("TechnicianTestTypes");
            ModelState.Remove("TestTypeConsumables");


            // --------------------------------------------------------
            // Basic validation
            // --------------------------------------------------------

            if (model.TestCategoryId <= 0)
            {
                ModelState.AddModelError(
                    "TestCategoryId",
                    "Please select a test category.");
            }


            if (string.IsNullOrWhiteSpace(model.RequiredSampleType))
            {
                ModelState.AddModelError(
                    "RequiredSampleType",
                    "Please select a required sample type.");
            }


            if (model.TurnaroundTimeHours <= 0)
            {
                ModelState.AddModelError(
                    "TurnaroundTimeHours",
                    "Turnaround time must be greater than zero.");
            }


            // --------------------------------------------------------
            // Find selected category
            // --------------------------------------------------------

            var selectedCategory = await _context.TestCategories
                .FirstOrDefaultAsync(c =>
                    c.TestCategoryId == model.TestCategoryId);

            if (selectedCategory == null)
            {
                ModelState.AddModelError(
                    "TestCategoryId",
                    "The selected test category does not exist.");
            }
            else
            {
                // IMPORTANT:
                // Your database still has the Category column.
                // Populate it from TestCategory.
                model.Category = selectedCategory.CategoryName;
            }


            // --------------------------------------------------------
            // Consumables validation
            // --------------------------------------------------------

            SelectedConsumableIds ??= new List<int>();
            ConsumableQuantities ??= new Dictionary<int, int>();


            // No duplicate selections
            if (SelectedConsumableIds.Count !=
                SelectedConsumableIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "A consumable cannot be selected more than once.");
            }


            // Every selected consumable must have a valid quantity
            foreach (var consumableId in SelectedConsumableIds)
            {
                if (!ConsumableQuantities.TryGetValue(
                        consumableId, out var qty) || qty <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a quantity greater than zero for every selected consumable.");

                    break;
                }
            }


            // Check that consumables actually exist
            if (SelectedConsumableIds.Any())
            {
                var existingConsumableIds =
                    await _context.Consumables
                        .Where(c =>
                            SelectedConsumableIds.Contains(c.ConsumableID))
                        .Select(c => c.ConsumableID)
                        .ToListAsync();

                var missingConsumables =
                    SelectedConsumableIds
                        .Except(existingConsumableIds)
                        .ToList();

                if (missingConsumables.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "One or more selected consumables no longer exist.");
                }
            }


            // ========================================================
            // IF VALID - SAVE
            // ========================================================

            if (ModelState.IsValid)
            {
                try
                {
                    // ------------------------------------------------
                    // Make sure Category is definitely populated
                    // ------------------------------------------------

                    if (string.IsNullOrWhiteSpace(model.Category))
                    {
                        model.Category = selectedCategory!.CategoryName;
                    }


                    // ------------------------------------------------
                    // Add TestType
                    // ------------------------------------------------

                    _context.TestTypes.Add(model);

                    await _context.SaveChangesAsync();


                    // ------------------------------------------------
                    // Add TestTypeConsumables
                    // ------------------------------------------------

                    foreach (var consumableId in SelectedConsumableIds)
                    {
                        var testTypeConsumable =
                            new TestTypeConsumable
                            {
                                TestTypeId = model.Id,
                                ConsumableId = consumableId,
                                QuantityRequired =
                                    ConsumableQuantities[consumableId]
                            };

                        _context.TestTypeConsumables.Add(
                            testTypeConsumable);
                    }


                    // ------------------------------------------------
                    // Save consumables
                    // ------------------------------------------------

                    await _context.SaveChangesAsync();


                    TempData["SuccessMessage"] =
                        "Test type created successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        "",
                        "Database error while saving the test type.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        "",
                        "An unexpected error occurred while saving the test type.");
                }
            }


            // ========================================================
            // IF INVALID - RETURN PAGE WITH DROPDOWNS
            // ========================================================

            await LoadCreateDropdowns();

            // Put the selected consumables back into ViewBag
            ViewBag.SelectedConsumableIds = SelectedConsumableIds;
            ViewBag.ConsumableQuantities = ConsumableQuantities;

            return View(model);
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


            await LoadCreateDropdowns();


            // --------------------------------------------------------
            // Existing consumables
            // --------------------------------------------------------

            ViewBag.SelectedConsumableIds =
                testType.TestTypeConsumables
                    .Select(tc => tc.ConsumableId)
                    .ToList();


            ViewBag.ConsumableQuantities =
                testType.TestTypeConsumables
                    .ToDictionary(
                        tc => tc.ConsumableId,
                        tc => tc.QuantityRequired);


            return View(testType);
        }


        // ============================================================
        // EDIT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
     int id,
     TestType model,
     List<int>? SelectedConsumableIds,
     List<int>? ConsumableQuantities)
        {
            // ============================================================
            // CHECK ID
            // ============================================================

            if (id != model.Id)
            {
                return NotFound();
            }


            // ============================================================
            // REMOVE NAVIGATION PROPERTY VALIDATION
            // ============================================================

            ModelState.Remove("TestCategory");
            ModelState.Remove("TestRequestItems");
            ModelState.Remove("TechnicianTestTypes");
            ModelState.Remove("TestTypeConsumables");


            // ============================================================
            // BASIC VALIDATION
            // ============================================================

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Test type name is required.");
            }


            if (model.TestCategoryId <= 0)
            {
                ModelState.AddModelError(
                    "TestCategoryId",
                    "Please select a test category.");
            }


            if (string.IsNullOrWhiteSpace(model.RequiredSampleType))
            {
                ModelState.AddModelError(
                    "RequiredSampleType",
                    "Please select a required sample type.");
            }


            if (model.TurnaroundTimeHours <= 0)
            {
                ModelState.AddModelError(
                    "TurnaroundTimeHours",
                    "Turnaround time must be greater than zero.");
            }


            // ============================================================
            // GET CATEGORY
            // ============================================================

            var selectedCategory = await _context.TestCategories
                .FirstOrDefaultAsync(c =>
                    c.TestCategoryId == model.TestCategoryId);


            if (selectedCategory == null)
            {
                ModelState.AddModelError(
                    "TestCategoryId",
                    "The selected test category does not exist.");
            }
            else
            {
                // Your database still contains the Category column.
                model.Category = selectedCategory.CategoryName;
            }


            // ============================================================
            // PREPARE CONSUMABLES
            // ============================================================

            SelectedConsumableIds ??= new List<int>();

            ConsumableQuantities ??= new List<int>();


            // ============================================================
            // VALIDATE CONSUMABLE COUNTS
            // ============================================================

            if (SelectedConsumableIds.Count != ConsumableQuantities.Count)
            {
                ModelState.AddModelError(
                    "",
                    "A consumable cannot be selected more than once.");
            }


            // ============================================================
            // VALIDATE QUANTITIES
            // ============================================================

            foreach (var consumableId in SelectedConsumableIds)
            {
                for (int i = 0;
                     i < SelectedConsumableIds.Count;
                     i++)
                {
                    if (ConsumableQuantities[i] <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Quantity for every selected consumable must be greater than zero.");
                    }
                }
            }


            // ============================================================
            // CHECK DUPLICATE CONSUMABLES
            // ============================================================

            if (SelectedConsumableIds.Count !=
                SelectedConsumableIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "A consumable cannot be selected more than once.");
            }


            // ============================================================
            // CHECK CONSUMABLES EXIST
            // ============================================================

            if (SelectedConsumableIds.Any())
            {
                var existingConsumableIds =
                    await _context.Consumables
                        .Where(c =>
                            SelectedConsumableIds.Contains(c.ConsumableID))
                        .Select(c => c.ConsumableID)
                        .ToListAsync();


                var missingConsumables =
                    SelectedConsumableIds
                        .Except(existingConsumableIds)
                        .ToList();


                if (missingConsumables.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "One or more selected consumables no longer exist.");
                }
            }


            // ============================================================
            // SAVE
            // ============================================================

            if (ModelState.IsValid)
            {
                try
                {
                    // ----------------------------------------------------
                    // GET EXISTING TEST TYPE
                    // ----------------------------------------------------

                    var existingTestType =
                        await _context.TestTypes
                            .FirstOrDefaultAsync(t => t.Id == id);


                    if (existingTestType == null)
                    {
                        return NotFound();
                    }


                    // ----------------------------------------------------
                    // UPDATE TEST TYPE
                    // ----------------------------------------------------

                    existingTestType.Name = model.Name;
                    existingTestType.Category = model.Category;
                    existingTestType.RequiredSampleType = model.RequiredSampleType;
                    existingTestType.UnitOfMeasurement = model.UnitOfMeasurement;
                    existingTestType.TurnaroundTimeHours = model.TurnaroundTimeHours;
                    existingTestType.ReferenceRangeLow = model.ReferenceRangeLow;
                    existingTestType.ReferenceRangeHigh = model.ReferenceRangeHigh;
                    existingTestType.TestCategoryId = model.TestCategoryId;


                    // ----------------------------------------------------
                    // REMOVE OLD CONSUMABLE RELATIONSHIPS
                    // ----------------------------------------------------

                    var existingConsumables =
                        await _context.TestTypeConsumables
                            .Where(tc => tc.TestTypeId == id)
                            .ToListAsync();


                    if (existingConsumables.Any())
                    {
                        _context.TestTypeConsumables.RemoveRange(
                            existingConsumables);
                    }


                    // ----------------------------------------------------
                    // ADD NEW CONSUMABLE RELATIONSHIPS
                    // ----------------------------------------------------

                    foreach (var consumableId in SelectedConsumableIds)
                    {
                        _context.TestTypeConsumables.Add(
                            new TestTypeConsumable
                            {
                                TestTypeId = id,
                                ConsumableId = consumableId,
                                QuantityRequired = ConsumableQuantities[consumableId]
                            });
                    }


                    // ----------------------------------------------------
                    // SAVE EVERYTHING
                    // ----------------------------------------------------

                    await _context.SaveChangesAsync();


                    TempData["SuccessMessage"] =
                        "Test type updated successfully.";


                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestTypeExists(model.Id))
                    {
                        return NotFound();
                    }


                    ModelState.AddModelError(
                        "",
                        "The test type was changed by another user. Please try again.");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Database error while updating the test type: "
                        + ex.InnerException?.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "An unexpected error occurred: "
                        + ex.Message);
                }
            }


            // ============================================================
            // IF VALIDATION FAILED
            // ============================================================

            await LoadCreateDropdowns();


            ViewBag.SelectedConsumableIds =
                SelectedConsumableIds;


            ViewBag.ConsumableQuantities =
                ConsumableQuantities;


            return View(model);
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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (testType == null)
            {
                return NotFound();
            }


            try
            {
                // ----------------------------------------------------
                // Remove TestTypeConsumables first
                // ----------------------------------------------------

                if (testType.TestTypeConsumables.Any())
                {
                    _context.TestTypeConsumables.RemoveRange(
                        testType.TestTypeConsumables);
                }


                // ----------------------------------------------------
                // Remove TestType
                // ----------------------------------------------------

                _context.TestTypes.Remove(testType);

                await _context.SaveChangesAsync();


                TempData["SuccessMessage"] =
                    "Test type deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "The test type cannot be deleted because it is being used by another record.";

                return RedirectToAction(nameof(Index));
            }
        }


        // ============================================================
        // HELPER - LOAD DROPDOWNS
        // ============================================================

        private async Task LoadCreateDropdowns()
        {
            // --------------------------------------------------------
            // Test Categories
            // --------------------------------------------------------

            var categories = await _context.TestCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.TestCategoryId =
                new SelectList(
                    categories,
                    "TestCategoryId",
                    "CategoryName");


            // --------------------------------------------------------
            // Sample Types
            // --------------------------------------------------------
            // Assumes SampleTypeLookup has Id + Name.
            // --------------------------------------------------------

            var sampleTypes =
                await _context.SampleTypeLookups
                    .OrderBy(s => s.Name)
                    .ToListAsync();

            ViewBag.SampleTypes =
                new SelectList(
                    sampleTypes,
                    "Name",
                    "Name");


            // --------------------------------------------------------
            // Consumables
            // --------------------------------------------------------

            var consumables =
                await _context.Consumables
                    .OrderBy(c => c.Name)
                    .ToListAsync();

            ViewBag.Consumables = consumables;
        }


        // ============================================================
        // EXISTS
        // ============================================================

        private bool TestTypeExists(int id)
        {
            return _context.TestTypes
                .Any(e => e.Id == id);
        }
    }
}