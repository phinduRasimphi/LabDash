namespace LabDash.ViewModels
{
    public class AlertsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string UrgencyFilter { get; set; } = "All";
        public List<AlertRow> Alerts { get; set; } = new();
    }

    public class AlertRow
    {
        public int ResultId { get; set; }
        public int RequestId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientIdNumber { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string TestCategory { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string Units { get; set; } = string.Empty;
        public string ReferenceRange { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime DateCaptured { get; set; }
        public string Urgency { get; set; } = "Routine";
        public string RequestStatus { get; set; } = "Submitted";
        public bool Verified { get; set; }
    }
}