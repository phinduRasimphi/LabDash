using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using LabDash.Enums;


namespace LabDash.Models
{
    public class TestRequestItem
    {
        [Key]
        public int TestRequestItemId { get; set; }

        public int RequestId { get; set; }
        public virtual TestRequest TestRequest { get; set; }

        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; }

        public string Status { get; set; } = "Submitted";

        public string AssignedTechnicianId { get; set; }
        public virtual LabUser AssignedTechnician { get; set; }

        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }
    }
}