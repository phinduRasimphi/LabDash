// ViewModels/ResultsViewModel.cs
namespace LabDash.ViewModels
{
    public class ResultsFolderListViewModel
    {
        public List<ResultsFolderSummaryViewModel> PatientFolders { get; set; } = new();
    }

    public class ResultsFolderSummaryViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientIDNumber { get; set; }
        public int CompletedRequestCount { get; set; }
        public bool HasAbnormalResult { get; set; }
    }

    public class PatientResultsViewModel
    {
        public string PatientName { get; set; }
        public string PatientIDNumber { get; set; }
        public List<TestRequestResultsViewModel> Requests { get; set; } = new();
    }

    public class TestRequestResultsViewModel
    {
        public int RequestId { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string? ReleaseNote { get; set; }
        public List<TestResultLineViewModel> Results { get; set; } = new();

        public bool CanRelease => Status == "Completed";
        public bool HasAbnormal => Results.Any(r => r.IsAbnormal);
    }

    public class TestResultLineViewModel
    {
        public string TestTypeName { get; set; }
        public string ResultValue { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
    }
}