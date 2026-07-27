using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Medication
    {
        public int MedicationId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Medication Name")]
        public string MedicationName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}