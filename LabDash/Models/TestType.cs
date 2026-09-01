using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Test name is required.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Keep this because the rest of your application uses TestType.Category
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Required sample type is required.")]
        public string RequiredSampleType { get; set; } = string.Empty;

        public string? UnitOfMeasurement { get; set; }

        [Required(ErrorMessage = "Turnaround time is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Turnaround time must be greater than 0.")]
        public int TurnaroundTimeHours { get; set; }

        public decimal? ReferenceRangeLow { get; set; }

        public decimal? ReferenceRangeHigh { get; set; }

        [Required(ErrorMessage = "Test category is required.")]
        public int TestCategoryId { get; set; }

        public virtual TestCategory? TestCategory { get; set; }

        public virtual ICollection<TestRequestItem> TestRequestItems { get; set; }
            = new List<TestRequestItem>();

        public virtual ICollection<TechnicianTestType> TechnicianTestTypes { get; set; }
            = new List<TechnicianTestType>();

        public virtual ICollection<TestTypeConsumable> TestTypeConsumables { get; set; }
            = new List<TestTypeConsumable>();

    }
}