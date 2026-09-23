using System.ComponentModel.DataAnnotations;

namespace LabDash.ViewModels
{
    public class DoctorProfile
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "HPCSA number is required.")]
        [StringLength(50)]
        public string HPCSANumber { get; set; } = "";

        [Required(ErrorMessage = "Employee number is required.")]
        [StringLength(50)]
        public string EmployeeNumber { get; set; } = "";

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(20)]
        public string PhoneNumb { get; set; } = "";

        public string? Gender { get; set; }

        [StringLength(50)]
        public string? SouthAfricanID { get; set; }
    }
}
