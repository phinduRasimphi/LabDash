namespace LabDash.ViewModels
{
    public class TestRequestSubmittedEmailViewModel
    {
        public string PatientFirstName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string TestList { get; set; } = "";
        public string RequestDate { get; set; } = "";
        public int RequestId { get; set; }
    }
}