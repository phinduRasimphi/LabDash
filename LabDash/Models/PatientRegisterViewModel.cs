using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class PatientRegisterViewModel
    {
        // ============================================================
        // PERSONAL INFORMATION
        // ============================================================

        [Required(ErrorMessage = "Please enter your first name.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "First name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[A-Za-zÀ-ÿ\s'-]+$",
            ErrorMessage = "First name can only contain letters.")]
        [Display(Name = "First Name")]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your surname.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Surname must be between 2 and 50 characters.")]
        [RegularExpression(@"^[A-Za-zÀ-ÿ\s'-]+$",
            ErrorMessage = "Surname can only contain letters.")]
        public string Surname { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your 13-digit South African ID number.")]
        [RegularExpression(@"^\d{13}$",
            ErrorMessage = "ID number must contain exactly 13 digits.")]
        [StringLength(13, MinimumLength = 13,
            ErrorMessage = "ID number must be exactly 13 digits.")]
        [Display(Name = "SA ID Number")]
        public string IDNumber { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your date of birth.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DOB { get; set; }


        // ============================================================
        // CONTACT
        // ============================================================

        [Required(ErrorMessage = "Please enter your cellphone number.")]
        [RegularExpression(@"^0[6-8][0-9]{8}$",
            ErrorMessage = "Cellphone number must be a valid 10-digit South African number, e.g. 0821234567.")]
        [Display(Name = "Cellphone Number")]
        public string CellphoneNumber { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your Gmail address.")]
        [RegularExpression(
            @"^[A-Za-z0-9._%+-]+@gmail\.com$",
            ErrorMessage = "Please use a valid Gmail address ending in @gmail.com.")]
        [Display(Name = "Gmail Address")]
        public string Email { get; set; } = string.Empty;


        // ============================================================
        // ADDRESS
        // ============================================================

        [Required(ErrorMessage = "Please enter your street address.")]
        [StringLength(200,
            ErrorMessage = "Street address cannot exceed 200 characters.")]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;


        [StringLength(200,
            ErrorMessage = "Address Line 2 cannot exceed 200 characters.")]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }


        [Required(ErrorMessage = "Please enter your suburb.")]
        [StringLength(100)]
        public string Suburb { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your city.")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter your province.")]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty;


        [RegularExpression(
            @"^\d{4}$",
            ErrorMessage = "Postal code must contain exactly 4 digits.")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }


        // Legacy field
        public string? HomeAddress { get; set; }


        // ============================================================
        // MEDICAL CONDITIONS
        // ============================================================

        [Display(Name = "Medical Conditions")]
        public List<int> SelectedMedicalConditionIds { get; set; }
            = new List<int>();


        public IEnumerable<SelectListItem> MedicalConditionOptions
        {
            get;
            set;
        } = new List<SelectListItem>();


        [StringLength(200,
            ErrorMessage = "Custom medical condition cannot exceed 200 characters.")]
        [Display(Name = "Other Medical Condition")]
        public string? OtherMedicalCondition { get; set; }


        // ============================================================
        // ALLERGIES
        // ============================================================

        [Display(Name = "Allergies")]
        public List<int> SelectedAllergyIds { get; set; }
            = new List<int>();


        public IEnumerable<SelectListItem> AllergyOptions
        {
            get;
            set;
        } = new List<SelectListItem>();


        [StringLength(200,
            ErrorMessage = "Custom allergy cannot exceed 200 characters.")]
        [Display(Name = "Other Allergy")]
        public string? OtherAllergy { get; set; }


        // ============================================================
        // MEDICATIONS
        // ============================================================

        [Display(Name = "Current Medications")]
        public List<int> SelectedMedicationIds { get; set; }
            = new List<int>();


        public IEnumerable<SelectListItem> MedicationOptions
        {
            get;
            set;
        } = new List<SelectListItem>();


        [StringLength(200,
            ErrorMessage = "Custom medication cannot exceed 200 characters.")]
        [Display(Name = "Other Medication")]
        public string? OtherMedication { get; set; }


        // ============================================================
        // LEGACY MEDICAL FIELDS
        // ============================================================

        public string? MedicalConditions { get; set; }

        public string? Allergies { get; set; }

        public string? Medication { get; set; }


        // ============================================================
        // ACCOUNT SECURITY
        // ============================================================

        [Required(ErrorMessage = "Please create a password.")]
        [DataType(DataType.Password)]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{8,}$",
            ErrorMessage =
                "Password must contain at least 8 characters, one uppercase letter, one number, and one special character.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "The confirmation password must match your password.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}