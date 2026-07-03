using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class SampleReceive
    {
        [Key]
        public int SampleReceptionId { get; set; }

        // Foreign Key
        [Required]
        public int RequestId { get; set; }

        public TestRequest? TestRequest { get; set; }

        [Required]
        [Display(Name = "Technician Name")]
        public string TechnicianName { get; set; }

        [Required]
        [Display(Name = "Sample Barcode")]
        public string SampleBarcode { get; set; }

        [Required]
        [Display(Name = "Sample Type")]
        public string SampleType { get; set; }

        [Display(Name = "Date & Time Received")]
        public DateTime DateTimeReceived { get; set; }

        public string Status { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}