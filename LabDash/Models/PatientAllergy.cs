using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class PatientAllergy
    {
        [Key]
        public int PatientAllergyId { get; set; }

        public int PatientID { get; set; }
        public Patient? Patient { get; set; }

        public int AllergyId { get; set; }
        public Allergy? Allergy { get; set; }

        // ── New fields for the upgraded Medical History view ──
        public DateTime? RecordedDate { get; set; }

        [StringLength(20)]
        public string? Severity { get; set; } // "Mild" | "Moderate" | "Severe"

        [ForeignKey(nameof(RecordedByDoctor))]
        public string? RecordedByDoctorId { get; set; }
        public LabUser? RecordedByDoctor { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}