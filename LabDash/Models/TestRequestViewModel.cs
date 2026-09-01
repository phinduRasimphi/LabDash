public class TestRequestResultsViewModel
{
    public int RequestId { get; set; }
    public DateTime RequestDate { get; set; }
    public string DoctorName { get; set; }
    public string Urgency { get; set; }
    public string? ClinicalNotes { get; set; }
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