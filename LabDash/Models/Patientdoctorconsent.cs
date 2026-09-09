using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    // A patient granting a specific doctor access to their results.
    // One row per patient-doctor pair; IsActive is toggled off on revoke
    // rather than deleting the row, so history is preserved.
    // "Doctor" here is a LabUser (Identity account) in the Doctor role,
    // the same pattern TestRequest.RequestingDoctor already uses.
    public class PatientDoctorConsent
    {
        [Key]
        public int ConsentID { get; set; }

        [ForeignKey(nameof(Patient))]
        public int PatientID { get; set; }
        public virtual Patient? Patient { get; set; }

        [Required]
        public string DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public virtual LabUser? Doctor { get; set; }

        public DateTime GrantedDate { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<ConsentRequestAccess> ConsentRequestAccesses { get; set; }
            = new List<ConsentRequestAccess>();
    }

    // Junction table: which specific TestRequests a given consent grant
    // covers. A patient can share only some of their requests with a doctor.
    public class ConsentRequestAccess
    {
        [Key]
        public int ConsentAccessID { get; set; }

        [ForeignKey(nameof(Consent))]
        public int ConsentID { get; set; }
        public PatientDoctorConsent? Consent { get; set; }

        [ForeignKey(nameof(TestRequest))]
        public int RequestID { get; set; }
        public TestRequest? TestRequest { get; set; }
    }
}
