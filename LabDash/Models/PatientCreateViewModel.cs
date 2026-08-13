using System.ComponentModel.DataAnnotations;

namespace LabDash.ViewModels
{
    public class PatientCreateViewModel
    {
        [Required, Display(Name = "Name")]
        public string Name { get; set; }

        [Required, Display(Name = "Surname")]
        public string Surname { get; set; }

        [Required, Display(Name = "South African ID Number")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "ID number must be 13 digits")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must contain digits only")]
        public string IDNumber { get; set; }

        [Required, Display(Name = "Cellphone Number")]
        [Phone]
        public string CellphoneNumber { get; set; }

        [Required, Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required, EmailAddress, Display(Name = "Email Address (Username)")]
        public string Email { get; set; }

        [Required, Display(Name = "Home Address")]
        public string HomeAddress { get; set; }

        [Display(Name = "Known Medical Conditions")]
        public string? MedicalConditions { get; set; }

        [Display(Name = "Allergies")]
        public string? Allergies { get; set; }

        [Display(Name = "Current Medication")]
        public string? Medication { get; set; }
    }
}