using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class SampleTypeLookup
    {
        [Key]
        public int SampleTypeLookupId { get; set; }

        [Required(ErrorMessage = "Sample type name is required.")]
        [StringLength(100)]
        [Display(Name = "Sample Type")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}