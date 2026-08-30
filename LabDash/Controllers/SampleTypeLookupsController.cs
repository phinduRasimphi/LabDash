using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class SampleTypeLookupsController : Controller
    {
        private readonly LabDbContext _context;

        public SampleTypeLookupsController(LabDbContext context)
        {
            _context = context;
        }

        // GET: SampleTypeLookups
        public async Task<IActionResult> Index()
        {
            var sampleTypes = await _context.SampleTypeLookups
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(sampleTypes);
        }

        // GET: SampleTypeLookups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sampleType = await _context.SampleTypeLookups
                .FirstOrDefaultAsync(s => s.SampleTypeLookupId == id);

            if (sampleType == null)
            {
                return NotFound();
            }

            return View(sampleType);
        }

        // GET: SampleTypeLookups/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SampleTypeLookups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SampleTypeLookup sampleType)
        {
            if (ModelState.IsValid)
            {
                // Prevent duplicate sample type names
                bool exists = await _context.SampleTypeLookups
                    .AnyAsync(s => s.Name.ToLower() == sampleType.Name.ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "Name",
                        "This sample type already exists.");

                    return View(sampleType);
                }

                sampleType.Name = sampleType.Name.Trim();

                _context.SampleTypeLookups.Add(sampleType);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Sample type added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(sampleType);
        }

        // GET: SampleTypeLookups/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sampleType = await _context.SampleTypeLookups
                .FindAsync(id);

            if (sampleType == null)
            {
                return NotFound();
            }

            return View(sampleType);
        }

        // POST: SampleTypeLookups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            SampleTypeLookup sampleType)
        {
            if (id != sampleType.SampleTypeLookupId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check duplicate name
                bool exists = await _context.SampleTypeLookups
                    .AnyAsync(s =>
                        s.SampleTypeLookupId != id &&
                        s.Name.ToLower() == sampleType.Name.ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "Name",
                        "This sample type already exists.");

                    return View(sampleType);
                }

                try
                {
                    sampleType.Name = sampleType.Name.Trim();

                    _context.Update(sampleType);

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Sample type updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SampleTypeExists(sampleType.SampleTypeLookupId))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            return View(sampleType);
        }

        // GET: SampleTypeLookups/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sampleType = await _context.SampleTypeLookups
                .FirstOrDefaultAsync(
                    s => s.SampleTypeLookupId == id);

            if (sampleType == null)
            {
                return NotFound();
            }

            return View(sampleType);
        }

        // POST: SampleTypeLookups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sampleType = await _context.SampleTypeLookups
                .FindAsync(id);

            if (sampleType != null)
            {
                _context.SampleTypeLookups.Remove(sampleType);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Sample type deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SampleTypeExists(int id)
        {
            return _context.SampleTypeLookups
                .Any(e => e.SampleTypeLookupId == id);
        }
    }
}