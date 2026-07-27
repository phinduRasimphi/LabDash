using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int PatientId { get; set; }

        public DateTime RequestDate { get; set; }

        public DateTime DateTimeReceived { get; set; }

        public string Urgency { get; set; }

        public string ClinicalNotes { get; set; }

        public string Status { get; set; }

        public DateTime SubmittedDate { get; set; }
        public virtual Patient Patient { get; set; }
        public string? RequestingDoctorId { get; set; }
        public virtual LabUser? RequestingDoctor { get; set; }
        public virtual ICollection<Sample> Samples { get; set; }
            = new List<Sample>();

        public virtual ICollection<SampleReceive> SampleReceives { get; set; }
            = new List<SampleReceive>();
        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; }
        = new List<TestRequestItem>();
    }
}