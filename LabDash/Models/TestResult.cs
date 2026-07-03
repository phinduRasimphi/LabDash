using LabDash.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestResult
    {
        [Key]
        public int ResultId { get; set; }

        public int TestRequestItemId { get; set; }

        public virtual TestRequestItem TestRequestItem { get; set; }

        [Required]
        public string ResultValue { get; set; }

        public string? Units { get; set; }

        public string? ReferenceRange { get; set; }

        public string? Comments { get; set; }

        public DateTime DateCaptured { get; set; }

        public string CapturedByTechnicianId { get; set; }

        public virtual LabUser CapturedByTechnician { get; set; }
    }
}