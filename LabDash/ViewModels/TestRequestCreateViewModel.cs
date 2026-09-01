namespace LabDash.ViewModels
{
    public class TestRequestCreateViewModel
    {
        public int PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string PatientIDNumber { get; set; } = string.Empty;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public string Urgency { get; set; } = "Routine";

        public string? ClinicalNotes { get; set; }

        public List<int> SelectedTestTypeIds { get; set; } = new();

        public List<TestTypeOptionViewModel> AvailableTestTypes { get; set; } = new();
    }

    public class TestTypeOptionViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string RequiredSampleType { get; set; } = string.Empty;

        public int TurnaroundTimeHours { get; set; }
    }
}