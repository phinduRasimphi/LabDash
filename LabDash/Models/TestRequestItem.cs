using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using LabDash.Enums;

namespace LabDash.Models
{
    public class TestRequestItem
    {
        [Key]
        public int TestRequestItemId { get; set; }

        public int RequestId { get; set; }
        public virtual TestRequest TestRequest { get; set; } = null!;

        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; } = null!;

        public string Status { get; set; } = "Submitted";

        public string? AssignedTechnicianId { get; set; }
        public virtual LabUser? AssignedTechnician { get; set; }

        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletionDateTime { get; set; }

        public virtual ICollection<TestResult> TestResults { get; set; }
            = new List<TestResult>();
    }
}