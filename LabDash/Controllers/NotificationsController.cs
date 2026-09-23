using LabDash.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabDash.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class NotificationsController : Controller
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public NotificationsController(LabDbContext context, UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // GET /Notifications/UnreadCount  → { count: n }
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var id = CurrentUserId;
            var count = await _context.Notifications
                .CountAsync(n => n.RecipientUserId == id && !n.IsRead);

            return Json(new { count });
        }

        // GET /Notifications/Latest  → list for the dropdown
        [HttpGet]
        public async Task<IActionResult> Latest(int take = 10)
        {
            var id = CurrentUserId;

            var items = await _context.Notifications
                .Where(n => n.RecipientUserId == id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new
                {
                    id = n.NotificationID,
                    title = n.Title,
                    message = n.Message,
                    url = n.LinkUrl,
                    type = n.Type,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                })
                .ToListAsync();

            return Json(items);
        }

        // POST /Notifications/MarkRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = CurrentUserId;

            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.NotificationID == id && x.RecipientUserId == userId);

            if (n == null) return NotFound();

            n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST /Notifications/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = CurrentUserId;

            var unread = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}