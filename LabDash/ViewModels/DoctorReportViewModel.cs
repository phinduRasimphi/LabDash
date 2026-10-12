namespace LabDash.ViewModels
{
    public class DoctorReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string StatusFilter { get; set; } = "All";
        public string DoctorName { get; set; } = "";
        public string DoctorHpcsa { get; set; } = "";
        public List<DoctorReportRow> Rows { get; set; } = new();
    }

    public class DoctorReportRow
    {
        public int RequestId { get; set; }
        public DateTime RequestDate { get; set; }
        public string PatientName { get; set; } = "";
        public string PatientIdNum { get; set; } = "";
        public string Urgency { get; set; } = "";
        public string Status { get; set; } = "";
        public int TestsCount { get; set; }
        public int AbnormalCount { get; set; }
    }
}