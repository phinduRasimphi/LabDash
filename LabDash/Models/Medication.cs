using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    public class Medication
    {
        public int MedicationId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Medication Name")]
        public string MedicationName { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}