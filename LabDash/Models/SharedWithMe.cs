namespace LabDash.ViewModels
{
    // A row in the "Shared With Me" list — one patient.
    public class SharedPatientViewModel
    {
        public int PatientID { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string IDNumber { get; set; } = "";
        public string Email { get; set; } = "";

        public string FullName => $"{Name} {Surname}";

        public int SharedItemCount { get; set; }
        public DateTime LastGrantedDate { get; set; }

        public string Initials =>
            $"{(Name.Length > 0 ? Name[0] : '?')}" +
            $"{(Surname.Length > 0 ? Surname[0] : '?')}";
    }

    public class SharedWithMeViewModel
    {
        public List<SharedPatientViewModel> Patients { get; set; } = new();
    }

    // One test result row shown to the doctor.
    public class SharedResultViewModel
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; } = "Routine";
        public string RequestStatus { get; set; } = "";

        public int TestRequestItemID { get; set; }
        public string TestName { get; set; } = "";
        public string Category { get; set; } = "";

        public string ResultValue { get; set; } = "";
        public string Unit { get; set; } = "";
        public bool IsAbnormal { get; set; }
        public DateTime? DateCaptured { get; set; }
        public string TechnicianNotes { get; set; } = "";

        public bool HasResult => !string.IsNullOrWhiteSpace(ResultValue);
    }

    public class PatientSharedResultsViewModel
    {
        public int PatientID { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string IDNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName => $"{Name} {Surname}";

        public List<SharedResultViewModel> Results { get; set; } = new();
        public int TotalItems => Results.Count;
        public int AbnormalCount => Results.Count(r => r.IsAbnormal);
    }
}