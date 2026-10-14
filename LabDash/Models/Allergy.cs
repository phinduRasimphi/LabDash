using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Allergy
    {
        public int AllergyId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Allergy Name")]
        public string AllergyName { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }   // navigation property, matches MedicalCondition

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}