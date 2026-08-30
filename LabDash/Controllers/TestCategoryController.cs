using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class TestCategoriesController : Controller
    {
        private readonly LabDbContext _context;

        public TestCategoriesController(LabDbContext context)
        {
            _context = context;
        }

        // GET: TestCategories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.TestCategories
                .Include(c => c.TestTypes)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(categories);
        }

        // GET: TestCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.TestCategories
                .Include(c => c.TestTypes)
                .FirstOrDefaultAsync(c => c.TestCategoryId == id);

            if (category == null)
                return NotFound();

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
        public async Task<IActionResult> Create(TestCategory testCategory)
        {
            if (await _context.TestCategories
                .AnyAsync(x => x.CategoryName == testCategory.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "A test category with this name already exists.");
            }

            if (!ModelState.IsValid)
                return View(testCategory);

            _context.TestCategories.Add(testCategory);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Test category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: TestCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.TestCategories
                .FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: TestCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            TestCategory testCategory)
        {
            if (id != testCategory.TestCategoryId)
                return NotFound();

            if (await _context.TestCategories.AnyAsync(x =>
                x.CategoryName == testCategory.CategoryName &&
                x.TestCategoryId != testCategory.TestCategoryId))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "A test category with this name already exists.");
            }

            if (!ModelState.IsValid)
                return View(testCategory);

            try
            {
                _context.Update(testCategory);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Test category updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TestCategoryExists(testCategory.TestCategoryId))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: TestCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.TestCategories
                .Include(c => c.TestTypes)
                .FirstOrDefaultAsync(c => c.TestCategoryId == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: TestCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.TestCategories
                .Include(c => c.TestTypes)
                .FirstOrDefaultAsync(c => c.TestCategoryId == id);

            if (category == null)
                return NotFound();

            // Prevent deleting a category that has test types
            if (category.TestTypes.Any())
            {
                TempData["Error"] =
                    "This category cannot be deleted because test types are assigned to it.";

                return RedirectToAction(nameof(Index));
            }

            _context.TestCategories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Test category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool TestCategoryExists(int id)
        {
            return _context.TestCategories
                .Any(e => e.TestCategoryId == id);
        }
    }
}