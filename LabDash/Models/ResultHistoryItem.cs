namespace LabDash.Models
{
    public class ResultHistoryItem
    {
        public int ResultId { get; set; }
        public string? ResultValue { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Comments { get; set; }
        public DateTime? CapturedOn { get; set; }
        public string? ReturnReason { get; set; }
        public DateTime? ReturnedOn { get; set; }
    }
}
