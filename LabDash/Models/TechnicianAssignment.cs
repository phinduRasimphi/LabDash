using System.ComponentModel.DataAnnotations;
using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class TechnicianAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        public string TechnicianId { get; set; }
        public virtual LabUser Technician { get; set; }

        public int TestTypeId { get; set; }
        public virtual TestType TestType { get; set; }
    }
}