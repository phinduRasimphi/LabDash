using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly LabDbContext _context;

        public SuppliersController(LabDbContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            return View(suppliers);
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Suppliers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            // Check if supplier name already exists
            bool exists = await _context.Suppliers
                .AnyAsync(s => s.SupplierName.ToLower() == supplier.SupplierName.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "SupplierName",
                    "A supplier with this name already exists."
                );
            }

            if (ModelState.IsValid)
            {
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Supplier added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.SupplierId)
            {
                return NotFound();
            }

            // Check duplicate supplier name
            bool exists = await _context.Suppliers
                .AnyAsync(s =>
                    s.SupplierId != supplier.SupplierId &&
                    s.SupplierName.ToLower() == supplier.SupplierName.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "SupplierName",
                    "A supplier with this name already exists."
                );
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Supplier updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.SupplierId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(supplier);
        }

        // GET: Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Supplier deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.SupplierId == id);
        }
    }
}