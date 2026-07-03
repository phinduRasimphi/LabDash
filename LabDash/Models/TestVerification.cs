using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestVerification
    {
        [Key]
        public int VerificationId { get; set; }

        public int TestRequestItemId { get; set; }

        public virtual TestRequestItem TestRequestItem { get; set; }

        // Technician performing the verification
        public string VerifiedByTechnicianId { get; set; }

        public virtual LabUser VerifiedByTechnician { get; set; }

        public DateTime VerificationDate { get; set; }

        // Verified / To Be Reviewed
        public string Status { get; set; }

        public string? VerificationNotes { get; set; }
    }
}