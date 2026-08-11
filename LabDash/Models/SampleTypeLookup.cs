using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class SampleTypeLookup
    {
        public int SampleTypeLookupId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}