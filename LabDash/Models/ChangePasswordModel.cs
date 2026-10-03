using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class ChangePasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;


        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;


        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}