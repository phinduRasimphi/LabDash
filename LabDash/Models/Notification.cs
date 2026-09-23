using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        // The doctor who should see this in their bell.
        [Required]
        public string RecipientUserId { get; set; } = "";

        [ForeignKey(nameof(RecipientUserId))]
        public LabUser? Recipient { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = "";

        [Required, StringLength(500)]
        public string Message { get; set; } = "";

        // e.g. "/Doctor/Consent/Details/12" — optional, can be null.
        [StringLength(300)]
        public string? LinkUrl { get; set; }

        // "ConsentGranted" | "NewPatient" | (add more later)
        [Required, StringLength(50)]
        public string Type { get; set; } = "";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Optional back-references (nullable so either can be null).
        public int? RelatedConsentID { get; set; }
        public int? RelatedPatientID { get; set; }

        // Who caused it (the patient's LabUser id) — useful for auditing.
        public string? ActorUserId { get; set; }
    }
}