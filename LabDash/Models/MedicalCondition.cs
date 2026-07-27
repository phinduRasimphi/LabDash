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

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}