using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string TableName { get; set; } = string.Empty;

        public string? Details { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}