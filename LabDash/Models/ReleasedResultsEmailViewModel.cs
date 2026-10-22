namespace LabDash.ViewModels
{
    public class ReleasedResultsEmailViewModel
    {
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int RequestId { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? ReleaseNote { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}