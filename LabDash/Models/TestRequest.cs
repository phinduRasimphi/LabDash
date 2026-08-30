using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    public class TestRequest
    {
        [Key]
        public int RequestId { get; set; }

        // Patient
        [Required]
        public int PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public virtual Patient Patient { get; set; }

        // Doctor who created the request
        [Required]
        public string RequestingDoctorId { get; set; }

        [ForeignKey(nameof(RequestingDoctorId))]
        public virtual LabUser RequestingDoctor { get; set; }

        // Request Details
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public string Urgency { get; set; }

        public string? ClinicalNotes { get; set; }

       
        [Required]
        public string Status { get; set; }

       
        public DateTime? DateTimeReceived { get; set; }

       
        public DateTime? SubmittedDate { get; set; }

      
        public string? CancellationReason { get; set; }
        // Add to TestRequest.cs, alongside the other properties
        public string? ReleaseNote { get; set; }
        public DateTime? ReleaseDate { get; set; }


        public virtual ICollection<Sample> Samples { get; set; }
            = new List<Sample>();

        public virtual ICollection<SampleReceive> SampleReceives { get; set; }
            = new List<SampleReceive>();

        
        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; }
            = new List<TestRequestItem>();
    }
}