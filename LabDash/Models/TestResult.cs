using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    public class TestResult
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int TestRequestItemId { get; set; }

        [ForeignKey(nameof(TestRequestItemId))]
        public virtual TestRequestItem TestRequestItem { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string ResultValue { get; set; } = "";

        [StringLength(50)]
        public string? Units { get; set; }

        [StringLength(100)]
        public string? ReferenceRange { get; set; }

        [StringLength(2000)]
        public string? Comments { get; set; }

        [Required]
        public DateTime DateCaptured { get; set; } = DateTime.Now;

        [Required]
        public string CapturedByTechnicianId { get; set; } = "";

        [ForeignKey(nameof(CapturedByTechnicianId))]
        public virtual LabUser CapturedByTechnician { get; set; } = null!;

        public bool IsAbnormal { get; set; }

        public string? VerifiedByTechnicianId { get; set; }

        [ForeignKey(nameof(VerifiedByTechnicianId))]
        public virtual LabUser? VerifiedByTechnician { get; set; }

        public DateTime? VerificationDate { get; set; }

        [StringLength(2000)]
        public string? VerificationNote { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = TestStatus.Completed;


        [NotMapped]
        public string PatientFullName =>
            TestRequestItem?.TestRequest?.Patient == null
                ? ""
                : $"{TestRequestItem.TestRequest.Patient.Name} {TestRequestItem.TestRequest.Patient.Surname}";

        [NotMapped]
        public string TestName =>
            TestRequestItem?.TestType?.Name ?? "";

        [NotMapped]
        public bool IsVerified => Status == TestStatus.Verified;

        [NotMapped]
        public bool IsAwaitingVerification => Status == TestStatus.Completed;

        [NotMapped]
        public bool IsReturnedForReview => Status == TestStatus.ToBeRevised;
    }
}