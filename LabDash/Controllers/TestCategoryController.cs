using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    // Restrict to Laboratory Manager role only
    // [Authorize(Roles = "LaboratoryManager")]
    public class TestCategoriesController : Controller
    {
        private readonly LabDbContext _context;

        public TestCategoriesController(LabDbContext context)
        {
            _context = context;
        }

        // GET: TestCategories
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.TestCategories
                .Include(c => c.TestTypes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CategoryName.Contains(searchTerm)
                                       || c.Description.Contains(searchTerm));
            }

            ViewData["SearchTerm"] = searchTerm;

            var categories = await query
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(categories);
        }

        // GET: TestCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.TestCategories
                .Include(c => c.TestTypes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // GET: TestCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TestCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryName,Description")] TestCategory category)
        {
            await ValidateUniqueName(category);

            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Test category '{category.CategoryName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: TestCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.TestCategories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: TestCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CategoryName,Description")] TestCategory category)
        {
            if (id != category.Id) return NotFound();

            await ValidateUniqueName(category);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Test category '{category.CategoryName}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CategoryExists(category.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: TestCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.TestCategories
                .Include(c => c.TestTypes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: TestCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.TestCategories
                .Include(c => c.TestTypes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // Prevent deletion if test types still reference this category
            if (category.TestTypes.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{category.CategoryName}' because " +
                    $"{category.TestTypes.Count} test type(s) are still assigned to it. " +
                    "Reassign or remove those test types first.";
                return RedirectToAction(nameof(Index));
            }

            _context.TestCategories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Test category '{category.CategoryName}' deleted.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CategoryExists(int id)
        {
            return await _context.TestCategories.AnyAsync(e => e.Id == id);
        }

        // Case-insensitive uniqueness check, excluding the current record when editing
        private async Task ValidateUniqueName(TestCategory category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName)) return;

            var nameExists = await _context.TestCategories
                .AnyAsync(c => c.Id != category.Id
                            && c.CategoryName.ToLower() == category.CategoryName.Trim().ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(nameof(TestCategory.CategoryName),
                    "A test category with this name already exists.");
            }
        }
    }
}
