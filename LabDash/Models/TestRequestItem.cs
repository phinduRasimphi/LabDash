using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class TestRequestItem
    {
        public int TestRequestItemId { get; set; }

        public int RequestId { get; set; }

        public TestRequest TestRequest { get; set; }

        public int TestTypeId { get; set; }

        public TestType TestType { get; set; }

        public string Status { get; set; } = "Submitted";

        public string AssignedTechnicianId { get; set; }

        public LabUser AssignedTechnician { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? CompletionDateTime { get; set; }
        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; }
    = new List<TestRequestItem>();
    }
}