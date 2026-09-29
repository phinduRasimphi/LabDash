using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class PatientRegisterViewModel
    {
        // ================= PERSONAL =================
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID number is required.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "ID number must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must contain only digits.")]
        public string IDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "Cellphone number is required.")]
        [RegularExpression(@"^0[6-8][0-9]{8}$",
            ErrorMessage = "Enter a valid SA cellphone number (e.g. 0821234567).")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        // ================= ADDRESS =================
        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "Suburb is required.")]
        [StringLength(100)]
        public string Suburb { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province is required.")]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty;

        [StringLength(4)]
        [RegularExpression(@"^\d{0,4}$", ErrorMessage = "Postal code must be 4 digits.")]
        public string? PostalCode { get; set; }

        // Legacy — kept so old code that reads HomeAddress still compiles
        public string? HomeAddress { get; set; }

        // ================= MEDICAL =================
        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? Medication { get; set; }

        // ================= ACCOUNT =================
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{8,}$",
            ErrorMessage = "Password needs 8+ characters, one uppercase letter, one number, and one special character.")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}