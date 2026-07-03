using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestType
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string RequiredSampleType { get; set; }

        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; }
            = new List<TestRequestItem>();

        public virtual ICollection<TechnicianTestType> TechnicianTestTypes { get; set; }
            = new List<TechnicianTestType>();
    }
}