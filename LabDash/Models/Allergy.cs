using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Allergy
    {
        public int AllergyId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Allergy Name")]
        public string AllergyName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}