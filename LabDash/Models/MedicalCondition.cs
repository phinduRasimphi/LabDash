using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class MedicalCondition
    {
        public int MedicalConditionId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Condition Name")]
        public string ConditionName { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }   // navigation property, not a string anymore

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}