using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class ConsumableController : Controller
    {
        private readonly LabDbContext _context;

        public ConsumableController(LabDbContext context)
        {
            _context = context;
        }

        // GET: Consumable
        public async Task<IActionResult> Index()
        {
            var consumables = await _context.Consumables
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(consumables);
        }

        // GET: Consumable/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consumable = await _context.Consumables
                .FirstOrDefaultAsync(c => c.ConsumableID == id);

            if (consumable == null)
            {
                return NotFound();
            }

            return View(consumable);
        }

        // GET: Consumable/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Consumable/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Consumable consumable)
        {
            if (ModelState.IsValid)
            {
                consumable.CreatedAt = DateTime.Now;
                consumable.UpdatedAt = DateTime.Now;

                _context.Consumables.Add(consumable);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Consumable added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(consumable);
        }

        // GET: Consumable/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consumable = await _context.Consumables
                .FindAsync(id);

            if (consumable == null)
            {
                return NotFound();
            }

            return View(consumable);
        }

        // POST: Consumable/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Consumable consumable)
        {
            if (id != consumable.ConsumableID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    consumable.UpdatedAt = DateTime.Now;

                    _context.Update(consumable);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Consumable updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConsumableExists(consumable.ConsumableID))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            return View(consumable);
        }

        // GET: Consumable/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consumable = await _context.Consumables
                .FirstOrDefaultAsync(c => c.ConsumableID == id);

            if (consumable == null)
            {
                return NotFound();
            }

            return View(consumable);
        }

        // POST: Consumable/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consumable = await _context.Consumables
                .FindAsync(id);

            if (consumable != null)
            {
                _context.Consumables.Remove(consumable);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Consumable deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Consumable/AdjustStock/5
        public async Task<IActionResult> AdjustStock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consumable = await _context.Consumables
                .FindAsync(id);

            if (consumable == null)
            {
                return NotFound();
            }

            return View(consumable);
        }

        // POST: Consumable/AdjustStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(
            int id,
            string adjustmentType,
            int quantity)
        {
            var consumable = await _context.Consumables
                .FindAsync(id);

            if (consumable == null)
            {
                return NotFound();
            }

            if (quantity < 0)
            {
                ModelState.AddModelError(
                    "quantity",
                    "Quantity cannot be negative.");

                return View(consumable);
            }

            switch (adjustmentType)
            {
                case "increase":
                    consumable.StockLevel += quantity;
                    break;

                case "decrease":

                    if (quantity > consumable.StockLevel)
                    {
                        ModelState.AddModelError(
                            "quantity",
                            "You cannot decrease stock below zero.");

                        return View(consumable);
                    }

                    consumable.StockLevel -= quantity;
                    break;

                case "set":
                    consumable.StockLevel = quantity;
                    break;

                default:

                    ModelState.AddModelError(
                        "adjustmentType",
                        "Invalid stock adjustment type.");

                    return View(consumable);
            }

            consumable.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Stock level updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool ConsumableExists(int id)
        {
            return _context.Consumables
                .Any(e => e.ConsumableID == id);
        }
    }
}