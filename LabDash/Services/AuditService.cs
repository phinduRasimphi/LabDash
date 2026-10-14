using LabDash.Areas.Identity.Data;
using LabDash.Models;

namespace LabDash.Services
{
    public interface IAuditService
    {
        void Log(
            string action,
            string tableName,
            string details);

        void LogAs(
            string userName,
            string action,
            string tableName,
            string details);
    }

    public class AuditService : IAuditService
    {
        private readonly LabDbContext _context;
        private readonly IHttpContextAccessor _http;

        public AuditService(
            LabDbContext context,
            IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        // Log using the currently authenticated user.
        public void Log(
            string action,
            string tableName,
            string details)
        {
            var userName =
                _http.HttpContext?.User?.Identity?.Name
                ?? "System";

            AddAuditLog(
                userName,
                action,
                tableName,
                details);
        }

        // Log using an explicitly specified user.
        // Useful for login, logout, registration and password resets.
        public void LogAs(
            string userName,
            string action,
            string tableName,
            string details)
        {
            AddAuditLog(
                string.IsNullOrWhiteSpace(userName)
                    ? "System"
                    : userName,
                action,
                tableName,
                details);
        }

        // Shared implementation for both logging methods.
        private void AddAuditLog(
            string userName,
            string action,
            string tableName,
            string details)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                ActionDate = DateTime.Now,
                UserName = userName,
                Action = action,
                TableName = tableName,
                Details = details
            });

            // Do not call SaveChanges here.
            // The caller saves the audit entry together
            // with its other database changes.
        }
    }
}