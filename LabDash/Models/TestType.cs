using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Category { get; set; }

        public string RequiredSampleType { get; set; }

        public string? UnitOfMeasurement { get; set; }

        public int TurnaroundTimeHours { get; set; }

        public decimal? ReferenceRangeLow { get; set; }

        public decimal? ReferenceRangeHigh { get; set; }

        public int TestCategoryId { get; set; }

        public virtual TestCategory TestCategory { get; set; }

        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; }
            = new List<TestRequestItem>();

        public virtual ICollection<TechnicianTestType> TechnicianTestTypes { get; set; }
            = new List<TechnicianTestType>();

        public virtual ICollection<TestTypeConsumable> TestTypeConsumables { get; set; }
            = new List<TestTypeConsumable>();
    }
}
