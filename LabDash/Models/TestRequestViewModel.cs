// ViewModels/TestRequestCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace LabDash.ViewModels
{
    public class TestRequestCreateViewModel
    {
        [Required]
        public int PatientId { get; set; }

        // Display-only, pulled from Patient for the form header
        public string PatientName { get; set; }
        public string PatientIDNumber { get; set; }

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required, Display(Name = "Urgency")]
        public string Urgency { get; set; }   // "Routine" | "Urgent" | "STAT"

        [Display(Name = "Clinical Notes")]
        public string? ClinicalNotes { get; set; }

        [Required(ErrorMessage = "Select at least one test type")]
        [MinLength(1, ErrorMessage = "Select at least one test type")]
        public List<int> SelectedTestTypeIds { get; set; } = new();

        // All available test types, for rendering checkboxes
        public List<TestTypeOptionViewModel> AvailableTestTypes { get; set; } = new();
    }

    public class TestTypeOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string RequiredSampleType { get; set; }
        public int TurnaroundTimeHours { get; set; }
    }
}
