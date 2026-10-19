namespace LabDash.ViewModels
{
    public class TestRequestCancelledEmailViewModel
    {
        public string PatientFirstName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public int RequestId { get; set; }
        public string CancellationReason { get; set; } = "";
    }
}