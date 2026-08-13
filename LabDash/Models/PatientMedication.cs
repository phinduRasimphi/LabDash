using System.ComponentModel.DataAnnotations;

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
    }
}