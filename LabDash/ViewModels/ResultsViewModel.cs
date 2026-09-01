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
}