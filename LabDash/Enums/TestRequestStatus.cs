namespace LabDash.Models
{
    public enum TestItemStatus
    {
        Submitted,
        InProgress,
        Completed,
        Verified,
        ToBeRevised
    }

    public enum RequestStatus
    {
        Submitted,
        SamplesReceived,
        InProgress,
        Completed,
        AppointmentScheduled,
        ReleasedByDoctor,
        Cancelled
    }
}