using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        public string UserId { get; set; }

        [Required] public string Name { get; set; }
        [Required] public string Surname { get; set; }
        [Required] public string IDNumber { get; set; }
        [Required] public string CellphoneNumber { get; set; }
        public DateTime DOB { get; set; }
        [Required] public string Email { get; set; }
        public string? Allergies { get; set; }
        [Required] public string HomeAddress { get; set; }

        public string? MedicalConditions { get; set; }

        public string? Medication { get; set; }
    }
}
