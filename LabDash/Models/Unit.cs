using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Unit
    {
        public int UnitId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}