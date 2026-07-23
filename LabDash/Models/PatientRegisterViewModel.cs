using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class PatientRegisterViewModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Surname { get; set; }
        [Required] public string IDNumber { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required, Phone]
        public string CellphoneNumber { get; set; }

        [Required] public string HomeAddress { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{8,}$",
            ErrorMessage = "Password needs 8+ characters, one uppercase letter, one number, and one special character.")]
        public string Password { get; set; }

        [DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}