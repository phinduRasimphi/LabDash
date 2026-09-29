using System.ComponentModel.DataAnnotations;

namespace LabDash.ViewModels
{
    public class PatientCreateViewModel
    {
        // ================= PERSONAL =================
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "South African ID number is required.")]
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
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // ================= ADDRESS =================
        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? Suburb { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

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

        // ================= DROPDOWN SOURCES =================
        public List<SelectOption> Provinces { get; set; } = new();
        public List<SelectOption> Cities { get; set; } = new();
        public List<SelectOption> Suburbs { get; set; } = new();

        // Full lookup lists (for JS to filter client-side)
        public List<MedicalOption> MedicalConditionOptions { get; set; } = new();
        public List<MedicalOption> AllergyOptions { get; set; } = new();
        public List<MedicalOption> MedicationOptions { get; set; } = new();

        // Category lists (distinct, for the category dropdown)
        public List<string> MedicalConditionCategories { get; set; } = new();
        public List<string> AllergyCategories { get; set; } = new();
        public List<string> MedicationCategories { get; set; } = new();

        // Selected values
        public int? SelectedMedicalConditionId { get; set; }
        public int? SelectedAllergyId { get; set; }
        public int? SelectedMedicationId { get; set; }
    }

    // ================= HELPER CLASSES (OUTSIDE the main class) =================

    public class SelectOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    public class MedicalOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}