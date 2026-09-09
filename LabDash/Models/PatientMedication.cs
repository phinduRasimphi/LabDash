using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class PatientMedication
    {
        [Key]
        public int PatientMedicationId { get; set; }

        public int PatientID { get; set; }
        public Patient? Patient { get; set; }

        public int MedicationId { get; set; }
        public Medication? Medication { get; set; }

        // ── New fields for the upgraded Medical History view ──
        [StringLength(100)]
        public string? Dosage { get; set; }

        [StringLength(100)]
        public string? Frequency { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; } // null = still active

        [ForeignKey(nameof(RecordedByDoctor))]
        public string? RecordedByDoctorId { get; set; }
        public LabUser? RecordedByDoctor { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}