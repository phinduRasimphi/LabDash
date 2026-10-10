
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LabDash.ViewModels
{
    public class PatientCreateViewModel
    {
        // PERSONAL DETAILS
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(50, MinimumLength = 2)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "South African ID number is required.")]
        [StringLength(13, MinimumLength = 13,
            ErrorMessage = "ID number must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$",
            ErrorMessage = "ID number must contain only digits.")]
        public string IDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "Cellphone number is required.")]
        [RegularExpression(@"^0[6-8][0-9]{8}$",
            ErrorMessage = "Enter a valid SA cellphone number.")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;


        // RESIDENTIAL ADDRESS
        [Required(ErrorMessage = "Please select a province.")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a city.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a suburb.")]
        public string Suburb { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a postal code.")]
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address line 1 is required.")]
        [StringLength(200)]
        public string? AddressLine1 { get; set; }

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        public List<SelectListItem> Provinces { get; set; } = new();


        // MEDICAL CONDITION OPTIONS AND CATEGORIES
        public List<MedicalOption> MedicalConditionOptions { get; set; } = new();

        public List<string> MedicalConditionCategories { get; set; } = new();

        public string? SelectedMedicalConditionCategory { get; set; }

        public int? SelectedMedicalConditionId { get; set; }

        // Retained for compatibility with existing code.
        public string? MedicalConditions { get; set; }


        // ALLERGY OPTIONS AND CATEGORIES
        public List<MedicalOption> AllergyOptions { get; set; } = new();

        public List<string> AllergyCategories { get; set; } = new();

        public string? SelectedAllergyCategory { get; set; }

        public int? SelectedAllergyId { get; set; }

        // Retained for compatibility with existing code.
        public string? Allergies { get; set; }


        // MEDICATION OPTIONS AND CATEGORIES
        public List<MedicalOption> MedicationOptions { get; set; } = new();

        public List<string> MedicationCategories { get; set; } = new();

        public string? SelectedMedicationCategory { get; set; }

        public int? SelectedMedicationId { get; set; }

        // Retained for compatibility with existing code.
        public string? Medication { get; set; }
    }


    // OPTION USED BY THE THREE MEDICAL DROPDOWNS
    public class MedicalOption
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}
