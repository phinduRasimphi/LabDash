using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Controllers
{
    public class ConsumableOrdersController : Controller
    {
        private readonly LabDbContext _context;

        public ConsumableOrdersController(LabDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX - SHOW ALL ORDERS
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var orders = await _context.ConsumableOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Consumable)
                .OrderByDescending(o => o.DateOrdered)
                .ToListAsync();

            return View(orders);
        }


        // =========================================================
        // DETAILS
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var order = await _context.ConsumableOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Consumable)
                .FirstOrDefaultAsync(o => o.ConsumableOrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        public IActionResult Create()
        {
            ViewBag.Suppliers = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName),
                "SupplierId",
                "SupplierName"
            );

            ViewBag.Consumables = _context.Consumables
                .OrderBy(c => c.Name)
                .ToList();

            return View();
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ConsumableOrder order,
            int[] consumableIds,
            int[] quantities)
        {
            if (consumableIds == null ||
                quantities == null ||
                consumableIds.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one consumable."
                );
            }

            if (consumableIds != null &&
                quantities != null &&
                consumableIds.Length != quantities.Length)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid consumable quantities."
                );
            }

            // Check duplicate order number
            if (!string.IsNullOrWhiteSpace(order.OrderNumber))
            {
                bool orderExists = await _context.ConsumableOrders
                    .AnyAsync(o => o.OrderNumber == order.OrderNumber);

                if (orderExists)
                {
                    ModelState.AddModelError(
                        "OrderNumber",
                        "This order number already exists."
                    );
                }
            }

            // Validate quantities
            if (quantities != null)
            {
                foreach (var quantity in quantities)
                {
                    if (quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "All quantities must be greater than zero."
                        );

                        break;
                    }
                }
            }

            if (ModelState.IsValid)
            {
                order.Status = "Ordered";

                if (order.DateOrdered == default)
                {
                    order.DateOrdered = DateTime.Now;
                }

                order.Items = new List<ConsumableOrderItem>();

                for (int i = 0; i < consumableIds.Length; i++)
                {
                    order.Items.Add(new ConsumableOrderItem
                    {
                        ConsumableId = consumableIds[i],
                        QuantityOrdered = quantities[i],
                        Status = "Ordered"
                    });
                }

                _context.ConsumableOrders.Add(order);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Consumable order created successfully.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Suppliers = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName),
                "SupplierId",
                "SupplierName",
                order.SupplierId
            );

            ViewBag.Consumables = _context.Consumables
                .OrderBy(c => c.Name)
                .ToList();

            return View(order);
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var order = await _context.ConsumableOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o =>
                    o.ConsumableOrderId == id);

            if (order == null)
                return NotFound();

            if (order.Status != "Ordered")
            {
                TempData["Error"] =
                    "Only ordered orders can be edited.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Suppliers = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName),
                "SupplierId",
                "SupplierName",
                order.SupplierId
            );

            ViewBag.Consumables = _context.Consumables
                .OrderBy(c => c.Name)
                .ToList();

            return View(order);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ConsumableOrder order,
            int[] consumableIds,
            int[] quantities)
        {
            if (id != order.ConsumableOrderId)
                return NotFound();

            var existingOrder =
                await _context.ConsumableOrders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o =>
                        o.ConsumableOrderId == id);

            if (existingOrder == null)
                return NotFound();

            if (existingOrder.Status != "Ordered")
            {
                TempData["Error"] =
                    "Only ordered orders can be edited.";

                return RedirectToAction(nameof(Index));
            }

            if (consumableIds == null ||
                consumableIds.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one consumable."
                );
            }

            if (quantities == null ||
                quantities.Length != consumableIds.Length)
            {
                ModelState.AddModelError(
                    "",
                    "Please provide a quantity for every consumable."
                );
            }

            if (quantities != null)
            {
                foreach (var quantity in quantities)
                {
                    if (quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Quantity must be greater than zero."
                        );

                        break;
                    }
                }
            }

            if (ModelState.IsValid)
            {
                existingOrder.SupplierId = order.SupplierId;
                existingOrder.OrderNumber = order.OrderNumber;
                existingOrder.DateOrdered = order.DateOrdered;

                _context.ConsumableOrderItems.RemoveRange(
                    existingOrder.Items
                );

                existingOrder.Items = new List<ConsumableOrderItem>();

                for (int i = 0; i < consumableIds.Length; i++)
                {
                    existingOrder.Items.Add(
                        new ConsumableOrderItem
                        {
                            ConsumableId = consumableIds[i],
                            QuantityOrdered = quantities[i],
                            Status = "Ordered"
                        }
                    );
                }

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Consumable order updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Suppliers = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName),
                "SupplierId",
                "SupplierName",
                order.SupplierId
            );

            ViewBag.Consumables = _context.Consumables
                .OrderBy(c => c.Name)
                .ToList();

            return View(order);
        }


        // =========================================================
        // RECEIVE ITEM
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveItem(int id)
        {
            var item = await _context.ConsumableOrderItems
                .Include(i => i.ConsumableOrder)
                .Include(i => i.Consumable)
                .FirstOrDefaultAsync(i =>
                    i.ConsumableOrderItemId == id);

            if (item == null)
                return NotFound();

            if (item.Status != "Ordered")
            {
                TempData["Error"] =
                    "This item has already been processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = item.ConsumableOrderId }
                );
            }

            // Update stock
            item.Consumable.StockLevel += item.QuantityOrdered;

            item.Status = "Received";
            item.DateReceived = DateTime.Now;

            await UpdateOrderStatus(item.ConsumableOrderId);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{item.Consumable.Name} has been received and stock updated.";

            return RedirectToAction(
                nameof(Details),
                new { id = item.ConsumableOrderId }
            );
        }


        // =========================================================
        // RECEIVE ENTIRE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrder(int id)
        {
            var order = await _context.ConsumableOrders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Consumable)
                .FirstOrDefaultAsync(o =>
                    o.ConsumableOrderId == id);

            if (order == null)
                return NotFound();

            foreach (var item in order.Items)
            {
                if (item.Status == "Ordered")
                {
                    item.Consumable.StockLevel +=
                        item.QuantityOrdered;

                    item.Status = "Received";
                    item.DateReceived = DateTime.Now;
                }
            }

            order.Status = "Complete";
            order.DateCompleted = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Order received successfully. Stock levels have been updated.";

            return RedirectToAction(
                nameof(Details),
                new { id = order.ConsumableOrderId }
            );
        }


        // =========================================================
        // CANCEL ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id,
            string cancellationReason)
        {
            var order = await _context.ConsumableOrders
                .FirstOrDefaultAsync(o =>
                    o.ConsumableOrderId == id);

            if (order == null)
                return NotFound();

            if (order.Status == "Complete")
            {
                TempData["Error"] =
                    "A completed order cannot be cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] =
                    "A cancellation reason is required.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            order.Status = "Cancelled";
            order.CancellationReason = cancellationReason;
            order.DateCancelled = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Order cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CANCEL ITEM
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelItem(
            int id,
            string cancellationReason)
        {
            var item = await _context.ConsumableOrderItems
                .FirstOrDefaultAsync(i =>
                    i.ConsumableOrderItemId == id);

            if (item == null)
                return NotFound();

            if (item.Status != "Ordered")
            {
                TempData["Error"] =
                    "Only ordered items can be cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = item.ConsumableOrderId }
                );
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] =
                    "A cancellation reason is required.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = item.ConsumableOrderId }
                );
            }

            item.Status = "Cancelled";
            item.CancellationReason = cancellationReason;
            item.DateCancelled = DateTime.Now;

            await UpdateOrderStatus(item.ConsumableOrderId);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Order item cancelled successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = item.ConsumableOrderId }
            );
        }


        // =========================================================
        // LOW STOCK
        // =========================================================

        public async Task<IActionResult> LowStock()
        {
            var consumables = await _context.Consumables
                .Where(c =>
                    c.StockLevel <=
                    (c.ReorderLevel * 1.10))
                .OrderBy(c => c.StockLevel)
                .ToListAsync();

            return View(consumables);
        }


        // =========================================================
        // UPDATE ORDER STATUS
        // =========================================================

        private async Task UpdateOrderStatus(int orderId)
        {
            var order = await _context.ConsumableOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o =>
                    o.ConsumableOrderId == orderId);

            if (order == null)
                return;

            var items = order.Items;

            if (items.Count == 0)
                return;

            int received = items.Count(i =>
                i.Status == "Received");

            int cancelled = items.Count(i =>
                i.Status == "Cancelled");

            int total = items.Count;

            if (received == total)
            {
                order.Status = "Complete";
                order.DateCompleted = DateTime.Now;
            }
            else if (received > 0)
            {
                order.Status = "Partially complete";
            }
            else if (cancelled == total)
            {
                order.Status = "Cancelled";
                order.DateCancelled = DateTime.Now;
            }
            else
            {
                order.Status = "Ordered";
            }
        }
    }
}