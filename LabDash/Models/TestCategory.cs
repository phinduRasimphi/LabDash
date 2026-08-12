using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }

        // Navigation property: one category has many test types
        public ICollection<TestType> TestTypes { get; set; } = new List<TestType>();
    }
}
