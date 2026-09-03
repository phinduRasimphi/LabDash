using LabDash.Models;

namespace LabDash.Models
{
    public class TrackRequestViewModel
    {
        public Patient Patient { get; set; }
        public List<TestRequest> Requests { get; set; }
    }
}
