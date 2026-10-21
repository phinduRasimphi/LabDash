namespace LabDash.Models
{
    /// <summary>
    /// Per-test-item / per-result progress statuses.
    /// Pipeline: Submitted -> In Progress -> Completed -> Verified
    ///                                     \-> To Be Reviewed -> Completed -> ...
    /// </summary>
    public static class TestStatus
    {
        public const string Submitted = "Submitted";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";      // captured by technician
        public const string Verified = "Verified";       // approved by 2nd technician
        public const string ToBeRevised = "To Be Reviewed"; // returned to technician
    }

    /// <summary>
    /// Request-level statuses (what the doctor sees in Track).
    /// </summary>
    public static class TestRequestStatus
    {
        public const string Submitted = "Submitted";
        public const string SamplesReceived = "Samples Received";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";            // ALL items Verified
        public const string AppointmentScheduled = "AppointmentScheduled";
        public const string ReleasedByDoctor = "Released by doctor";
        public const string Cancelled = "Cancelled";
    }
}