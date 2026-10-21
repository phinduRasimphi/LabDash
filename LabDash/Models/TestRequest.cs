using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    public class TestRequest
    {
        [Key]
        public int RequestId { get; set; }

        // ---------------- Patient ----------------
        [Required]
        public int PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public virtual Patient Patient { get; set; } = null!;

        // ---------------- Doctor ----------------
        [Required]
        public string RequestingDoctorId { get; set; } = "";

        [ForeignKey(nameof(RequestingDoctorId))]
        public virtual LabUser RequestingDoctor { get; set; } = null!;

        // ---------------- Details ----------------
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(20)]
        public string Urgency { get; set; } = "Routine";     // Routine | Urgent | STAT

        [StringLength(2000)]
        public string? ClinicalNotes { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = TestRequestStatus.Submitted;

        // ---------------- Timestamps ----------------
        public DateTime? SubmittedDate { get; set; }          // when doctor submitted
        public DateTime? DateTimeReceived { get; set; }       // when lab received samples

        // ---------------- Cancellation ----------------
        [StringLength(1000)]
        public string? CancellationReason { get; set; }
        public DateTime? CancelledDate { get; set; }

        // ---------------- Release ----------------
        [StringLength(2000)]
        public string? ReleaseNote { get; set; }
        public DateTime? ReleaseDate { get; set; }

        // ---------------- Appointment ----------------
        public DateTime? AppointmentDate { get; set; }

        [StringLength(200)]
        public string? AppointmentLocation { get; set; }

        [StringLength(500)]
        public string? AppointmentNote { get; set; }

        // ---------------- Samples / barcodes ----------------
        [StringLength(500)]
        public string? SampleBarcodes { get; set; }

        public DateTime? ResultsEmailedAt { get; set; }

        public virtual ICollection<Sample> Samples { get; set; } = new List<Sample>();
        public virtual ICollection<SampleReceive> SampleReceives { get; set; } = new List<SampleReceive>();
        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; } = new List<TestRequestItem>();

      

        [NotMapped]
        public bool AllItemsVerified =>
            TestRequestItems != null &&
            TestRequestItems.Count > 0 &&
            TestRequestItems.All(i => i.Status == TestStatus.Verified);

        [NotMapped]
        public bool AllItemsCompleted =>
            TestRequestItems != null &&
            TestRequestItems.Count > 0 &&
            TestRequestItems.All(i =>
                i.Status == TestStatus.Completed ||
                i.Status == TestStatus.Verified);

        [NotMapped]
        public bool CanBeReleased =>
            AllItemsVerified &&
            Status != TestRequestStatus.Cancelled &&
            Status != TestRequestStatus.ReleasedByDoctor;

        [NotMapped]
        public bool CanBeCancelled =>
            Status == TestRequestStatus.Submitted ||
            Status == TestRequestStatus.SamplesReceived;
    }
}