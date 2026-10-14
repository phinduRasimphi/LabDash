namespace LabDash.ViewModels
{
    public class TrackRequestViewModel
    {
        public LabDash.Models.Patient? Patient { get; set; }
        public List<LabDash.Models.TestRequest> Requests { get; set; } = new();
    }
}