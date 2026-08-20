using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestCategory
    {
        [Key]
        public int TestCategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<TestType> TestTypes { get; set; }
            = new List<TestType>();
    }
}