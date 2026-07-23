using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using LabDash.Enums;

namespace LabDash.Models
{
    public class TestVerification
    {
        [Key]
        public int VerificationId { get; set; }

        public int TestRequestItemId { get; set; }

        public virtual TestRequestItem TestRequestItem { get; set; }

        public string VerifiedByTechnicianId { get; set; }

        public virtual LabUser VerifiedByTechnician { get; set; }

        public DateTime VerificationDate { get; set; }

        public string Status { get; set; }

        public string? VerificationNotes { get; set; }
    }
}