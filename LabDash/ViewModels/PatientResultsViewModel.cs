namespace LabDash.ViewModels
{
    public class PatientResultsViewModel
    {
        public string PatientName { get; set; }

        public string PatientIDNumber { get; set; }

        public List<TestRequestResultsViewModel> Requests { get; set; } = new();
    }
}