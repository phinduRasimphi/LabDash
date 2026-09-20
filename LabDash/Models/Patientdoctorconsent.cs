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

        // LEGACY — whole-request grants. GrantConsent no longer writes to
        // this; it's kept only so older rows/migrations aren't broken.
        // Access is now controlled at the individual test-item level via
        // ConsentItemAccesses below.
        public virtual ICollection<ConsentRequestAccess> ConsentRequestAccesses { get; set; }
            = new List<ConsentRequestAccess>();

        // NEW — which specific test-request ITEMS (e.g. just "Glucose" out
        // of a Full Blood Count request) this doctor currently has access
        // to. A row existing here IS the access; deleting it revokes it
        // instantly.
        public virtual ICollection<ConsentItemAccess> ConsentItemAccesses { get; set; }
            = new List<ConsentItemAccess>();
    }

    // LEGACY junction table: whole-request access. Superseded by
    // ConsentItemAccess — left in place only for backward compatibility
    // with any existing data.
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

    // NEW junction table: which specific TestRequestItems (individual
    // tests within a request — e.g. Glucose or Cholesterol inside a
    // Full Blood Count request) a given consent grant covers. There is no
    // separate "IsActive" flag at this level — row presence IS access, so
    // revoking one test is just deleting its row, enforced instantly
    // anywhere that queries this table.
    public class ConsentItemAccess
    {
        [Key]
        public int ConsentItemAccessID { get; set; }

        [ForeignKey(nameof(Consent))]
        public int ConsentID { get; set; }
        public virtual PatientDoctorConsent? Consent { get; set; }

        [ForeignKey(nameof(TestRequestItem))]
        public int TestRequestItemID { get; set; }
        public virtual TestRequestItem? TestRequestItem { get; set; }
    }
}