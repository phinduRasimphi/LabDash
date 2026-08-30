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
            List<int>? ConsumableQuantities)
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
            // Validate consumables
            // --------------------------------------------------------

            SelectedConsumableIds ??= new List<int>();
            ConsumableQuantities ??= new List<int>();


            if (SelectedConsumableIds.Count != ConsumableQuantities.Count)
            {
                ModelState.AddModelError(
                    "",
                    "Please provide a quantity for every selected consumable.");
            }


            // --------------------------------------------------------
            // Validate every selected consumable
            // --------------------------------------------------------

            if (SelectedConsumableIds.Count == ConsumableQuantities.Count)
            {
                for (int i = 0; i < SelectedConsumableIds.Count; i++)
                {
                    int quantity = ConsumableQuantities[i];

                    if (quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Quantity for every selected consumable must be greater than zero.");
                    }
                }
            }


            // --------------------------------------------------------
            // Validate duplicate consumables
            // --------------------------------------------------------

            if (SelectedConsumableIds.Count !=
                SelectedConsumableIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "A consumable cannot be selected more than once.");
            }


            // --------------------------------------------------------
            // Check that consumables actually exist
            // --------------------------------------------------------

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

                    for (int i = 0;
                         i < SelectedConsumableIds.Count;
                         i++)
                    {
                        var testTypeConsumable =
                            new TestTypeConsumable
                            {
                                TestTypeId = model.Id,

                                ConsumableId =
                                    SelectedConsumableIds[i],

                                QuantityRequired =
                                    ConsumableQuantities[i]
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
            ViewBag.SelectedConsumableIds =
                SelectedConsumableIds;

            ViewBag.ConsumableQuantities =
                ConsumableQuantities;

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
                    .Select(tc => tc.QuantityRequired)
                    .ToList();


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
            if (id != model.Id)
            {
                return NotFound();
            }


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
            // Get category
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
                model.Category = selectedCategory.CategoryName;
            }


            // --------------------------------------------------------
            // Consumables
            // --------------------------------------------------------

            SelectedConsumableIds ??= new List<int>();
            ConsumableQuantities ??= new List<int>();


            if (SelectedConsumableIds.Count !=
                ConsumableQuantities.Count)
            {
                ModelState.AddModelError(
                    "",
                    "Please provide a quantity for every selected consumable.");
            }


            if (SelectedConsumableIds.Count ==
                ConsumableQuantities.Count)
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


            if (SelectedConsumableIds.Count !=
                SelectedConsumableIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "A consumable cannot be selected more than once.");
            }


            // ========================================================
            // SAVE EDIT
            // ========================================================

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTestType =
                        await _context.TestTypes
                            .FirstOrDefaultAsync(t => t.Id == id);

                    if (existingTestType == null)
                    {
                        return NotFound();
                    }


                    // ------------------------------------------------
                    // Update main TestType
                    // ------------------------------------------------

                    existingTestType.Name =
                        model.Name;

                    existingTestType.Category =
                        model.Category;

                    existingTestType.RequiredSampleType =
                        model.RequiredSampleType;

                    existingTestType.UnitOfMeasurement =
                        model.UnitOfMeasurement;

                    existingTestType.TurnaroundTimeHours =
                        model.TurnaroundTimeHours;

                    existingTestType.ReferenceRangeLow =
                        model.ReferenceRangeLow;

                    existingTestType.ReferenceRangeHigh =
                        model.ReferenceRangeHigh;

                    existingTestType.TestCategoryId =
                        model.TestCategoryId;


                    // ------------------------------------------------
                    // Remove existing consumables
                    // ------------------------------------------------

                    var existingConsumables =
                        await _context.TestTypeConsumables
                            .Where(tc => tc.TestTypeId == id)
                            .ToListAsync();

                    if (existingConsumables.Any())
                    {
                        _context.TestTypeConsumables.RemoveRange(
                            existingConsumables);
                    }


                    // ------------------------------------------------
                    // Add updated consumables
                    // ------------------------------------------------

                    for (int i = 0;
                         i < SelectedConsumableIds.Count;
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
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        "",
                        "Database error while updating the test type.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        "",
                        "An unexpected error occurred while updating the test type.");
                }
            }


            // ========================================================
            // RETURN EDIT PAGE
            // ========================================================

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
            //
            // IMPORTANT:
            // This assumes SampleTypeLookup has:
            //
            // Id
            // Name
            //
            // If your model uses different names, change them here.
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

            ViewBag.Consumables =
                consumables;
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