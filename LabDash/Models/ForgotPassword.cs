using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}