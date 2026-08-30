namespace LabDash.Models
{
    // ViewModels/TrackRequestsViewModel.cs
    namespace LabDash.ViewModels
    {
        public class TrackRequestsViewModel
        {
            public List<PatientFolderSummaryViewModel> PatientFolders { get; set; } = new();
        }

        public class PatientFolderSummaryViewModel
        {
            public int PatientId { get; set; }
            public string PatientName { get; set; }
            public string PatientIDNumber { get; set; }   // used server-side to verify unlock attempts
            public int RequestCount { get; set; }
        }

        public class PatientRequestsViewModel
        {
            public string PatientName { get; set; }
            public string PatientIDNumber { get; set; }
            public List<TestRequestTrackViewModel> Requests { get; set; } = new();
        }

        public class TestRequestTrackViewModel
        {
            public int RequestId { get; set; }
            public DateTime RequestDate { get; set; }
            public string Urgency { get; set; }
            public string Status { get; set; }
            public List<string> TestTypeNames { get; set; } = new();

            public bool CanCancel => Status == "Submitted" || Status == "Samples Received";
        }
    }
}