using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class PatientMedicalCondition
    {
        [Key]
        public int PatientMedicalConditionId { get; set; }

        public int PatientID { get; set; }
        public Patient? Patient { get; set; }

        public int MedicalConditionId { get; set; }
        public MedicalCondition? MedicalCondition { get; set; }

        // ── New fields for the upgraded Medical History view ──
        public DateTime? DiagnosisDate { get; set; }

        [StringLength(20)]
        public string? Severity { get; set; } // "Mild" | "Moderate" | "Severe"

        [ForeignKey(nameof(RecordedByDoctor))]
        public string? RecordedByDoctorId { get; set; }
        public LabUser? RecordedByDoctor { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}