using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Services
{
    public class NotificationService
    {
        private readonly LabDbContext _context;
        private readonly UserManager<LabUser> _userManager;

        public NotificationService(LabDbContext context, UserManager<LabUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Send to a specific doctor.
        public async Task SendToUserAsync(
            string recipientUserId,
            string title,
            string message,
            string type,
            string? linkUrl = null,
            int? relatedConsentId = null,
            int? relatedPatientId = null,
            string? actorUserId = null)
        {
            if (string.IsNullOrWhiteSpace(recipientUserId)) return;

            _context.Notifications.Add(new Notification
            {
                RecipientUserId = recipientUserId,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                RelatedConsentID = relatedConsentId,
                RelatedPatientID = relatedPatientId,
                ActorUserId = actorUserId,
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            await _context.SaveChangesAsync();
        }

        // Fan out to every user in a role (used for "new patient" alerts).
        public async Task SendToRoleAsync(
            string roleName,
            string title,
            string message,
            string type,
            string? linkUrl = null,
            int? relatedPatientId = null,
            string? actorUserId = null)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            if (usersInRole == null || usersInRole.Count == 0) return;

            foreach (var user in usersInRole)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    LinkUrl = linkUrl,
                    RelatedPatientID = relatedPatientId,
                    ActorUserId = actorUserId,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}