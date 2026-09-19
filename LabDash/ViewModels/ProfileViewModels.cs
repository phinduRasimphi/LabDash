using System.ComponentModel.DataAnnotations;

namespace LabDash.Models.ViewModels
{
    public class ProfileViewModel
    {
        // ============================================================
        // PROFILE INFORMATION
        // ============================================================

        public string? UserName { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        public string FullName =>
            $"{FirstName} {LastName}".Trim();

        public DateTime AccountCreated { get; set; }


        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword",
            ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }
    }
}
