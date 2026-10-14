namespace LabDash.ViewModels
{
    public class PatientResultRowViewModel
    {
        public string TestName { get; set; } = "";
        public string Category { get; set; } = "";
        public string ResultValue { get; set; } = "";
        public string Unit { get; set; } = "";
        public string RangeText { get; set; } = "-";
        public bool IsAbnormal { get; set; }
        public string Comments { get; set; } = "";

        public bool HasResult => !string.IsNullOrWhiteSpace(ResultValue);
    }

    public class PatientRequestDetailsViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string DoctorName { get; set; } = "Unknown";
        public string Urgency { get; set; } = "Routine";
        public string Status { get; set; } = "Submitted";

        // submitted | inprogress | completed | released ...
        public string StatusCssClass { get; set; } = "submitted";
        public string UrgencyCssClass { get; set; } = "routine";

        // 0 = cancelled, 1..4 = Submitted, In progress, Completed, Released
        public int Step { get; set; } = 1;

        public bool ResultsReleased { get; set; }
        public bool IsCancelled { get; set; }
        public string CancelReason { get; set; } = "";
        public DateTime? ReleaseDate { get; set; }
        public string ReleaseNote { get; set; } = "";

        public List<PatientResultRowViewModel> Items { get; set; } = new();
    }
}