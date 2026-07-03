using LabDash.Areas.Identity.Data;

namespace LabDash.Models
{
    public class TechnicianTestType
    {
        public int TechnicianTestTypeId { get; set; }

        public string TechnicianId { get; set; }

        public LabUser Technician { get; set; }

        public int TestTypeId { get; set; }

        public TestType TestType { get; set; }
    }
}