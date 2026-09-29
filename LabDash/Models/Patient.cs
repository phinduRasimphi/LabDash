using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [StringLength(13)]
        public string IDNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DOB { get; set; }

        [Required]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // ---- Structured address ----
        [StringLength(200)]
        public string? AddressLine1 { get; set; }

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? Suburb { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(4)]
        public string? PostalCode { get; set; }

        // ---- Legacy ----
        [StringLength(500)]
        public string? HomeAddress { get; set; }

        // ---- Medical ----
        public string? Allergies { get; set; }
        public string? MedicalConditions { get; set; }
        public string? Medication { get; set; }
    }
}