using System.ComponentModel.DataAnnotations;

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
    }
}