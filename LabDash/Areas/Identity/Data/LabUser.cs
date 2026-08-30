using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LabDash.Areas.Identity.Data;

// Add profile data for application users by adding properties to the LabUser class
public class LabUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    [DataType(DataType.Text)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    [DataType(DataType.Text)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";


    [StringLength(50)]
    [DataType(DataType.Text)]
    public string? Gender { get; set; }



    [Required(ErrorMessage = "Phone Number is required")]
    [Display(Name = "Phone Number")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^1?[0-9]{10}$", ErrorMessage = "Not a valid Phone number")]
    public string PhoneNumb { get; set; }

    [Required]
    [StringLength(13, MinimumLength = 13)]
    [Display(Name = "South African ID Number")]
    public string? SouthAfricanID { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Employee Number")]
    public string? EmployeeNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "HPCSA Number")]
    public string? HPCSANumber { get; set; }


    [Required]
    [Display(Name = "Creation Date of Account")]
    [DataType(DataType.DateTime)]
    public DateTime Timestamp_AccountCreated { get; set; } = DateTime.Now;
    // Add to LabUser.cs
    [Display(Name = "Must Change Password")]
    public bool MustChangePassword { get; set; } = true;
}

