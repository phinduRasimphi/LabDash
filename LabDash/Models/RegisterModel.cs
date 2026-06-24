using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(50)]
        [DataType(DataType.Text)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(50)]
        [DataType(DataType.Text)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [StringLength(50)]
        [DataType(DataType.Text)]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^1?[0-9]{10}$", ErrorMessage = "Not a valid Phone number.")]
        public string PhoneNumb { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "Password123!";



        [Required(ErrorMessage = "Creation Date of Account is required.")]
        [Display(Name = "Creation Date of Account")]
        [DataType(DataType.DateTime)]
        public DateTime Timestamp_AccountCreated { get; set; } = DateTime.Now;
    }
}
