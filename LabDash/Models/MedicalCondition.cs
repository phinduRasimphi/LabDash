using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class MedicalCondition
    {
        public int MedicalConditionId { get; set; }

        

        public int CategoryId { get; set; }
        public Category? Category { get; set; }   // navigation property, not a string anymore

       

        public bool IsActive { get; set; } = true;
        [Required, StringLength(200)]
        public string ConditionName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}