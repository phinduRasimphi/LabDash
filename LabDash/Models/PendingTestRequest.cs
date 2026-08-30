// ViewModels/CaptureResultsViewModel.cs
namespace LabDash.ViewModels
{
    public class PendingTestItemViewModel
    {
        public int TestRequestItemId { get; set; }
        public string PatientName { get; set; }
        public string TestTypeName { get; set; }
        public string RequiredSampleType { get; set; }
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; }
        public decimal? ReferenceRangeLow { get; set; }
        public decimal? ReferenceRangeHigh { get; set; }
    }

    public class CaptureResultViewModel
    {
        public int TestRequestItemId { get; set; }
        public string PatientName { get; set; }
        public string TestTypeName { get; set; }
        public decimal? ReferenceRangeLow { get; set; }
        public decimal? ReferenceRangeHigh { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string ResultValue { get; set; }

        public string? Units { get; set; }
        public string? Comments { get; set; }
        public bool IsAbnormal { get; set; }
    }
}