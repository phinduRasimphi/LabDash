using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class SampleType
    {
        [Key]
        public int SampleTypeId { get; set; }

        [Required(ErrorMessage = "Sample type name is required.")]
        [StringLength(100)]
        [Display(Name = "Sample Type")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}