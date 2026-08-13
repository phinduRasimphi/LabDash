using System.ComponentModel.DataAnnotations;

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
    }
}