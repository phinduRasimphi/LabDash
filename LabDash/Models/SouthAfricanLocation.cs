using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class SouthAfricanLocation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Province { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Suburb { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;
    }
}