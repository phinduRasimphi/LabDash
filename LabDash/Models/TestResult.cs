using System.ComponentModel.DataAnnotations;
using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class TestResult
    {
        [Key]
        public int ResultId { get; set; }

        public int TestRequestItemId { get; set; }
        public virtual TestRequestItem TestRequestItem { get; set; } = null!;

        [Required]
        public string ResultValue { get; set; } = "";

        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string? Comments { get; set; }
        public DateTime DateCaptured { get; set; }

        public string CapturedByTechnicianId { get; set; } = "";
        public virtual LabUser CapturedByTechnician { get; set; } = null!;

        public bool IsAbnormal { get; set; }

        public string? VerifiedByTechnicianId { get; set; }
        public virtual LabUser? VerifiedByTechnician { get; set; }

        public DateTime? VerificationDate { get; set; }
        public string? VerificationNote { get; set; }

        public string Status { get; set; } = "Completed";

        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string PatientFullName =>
            TestRequestItem?.TestRequest?.Patient == null
                ? ""
                : $"{TestRequestItem.TestRequest.Patient.Name} {TestRequestItem.TestRequest.Patient.Surname}";

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string TestName =>
            TestRequestItem?.TestType?.Name ?? "";
    }
}