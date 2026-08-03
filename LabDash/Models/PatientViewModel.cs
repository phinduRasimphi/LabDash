

using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    // ── 1. Profile ───────────────────────────────────────────
    public class PatientProfileViewModel
    {
        public int PatientID { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Surname is required.")]
        public string Surname { get; set; } = "";

        [Required(ErrorMessage = "ID Number is required.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "SA ID number must be 13 digits.")]
        public string IDNumber { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Cellphone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string Cellphone { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = "";

        public string HomeAddress { get; set; } = "";

        // Computed helper for the sidebar avatar
        public string Initials => $"{Name.FirstOrDefault()}{Surname.FirstOrDefault()}";
        public string FullName => $"{Name} {Surname}";
    }

    // ── 2. Test Request ──────────────────────────────────────
    public class TestRequestViewModel
    {
        public string RequestID { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string DoctorName { get; set; } = "";
        public List<string> Tests { get; set; } = new();
        public string Urgency { get; set; } = "Routine";  // Routine | Urgent | Stat
        public string Status { get; set; } = "Submitted";
        // Submitted | Samples Received | In Progress | Completed | Released | Cancelled

        // Display helpers
        public string UrgencyCssClass => Urgency.ToLower() switch
        {
            "stat" => "urgency-stat",
            "urgent" => "urgency-urgent",
            _ => "urgency-routine"
        };

        public string StatusCssClass => Status.ToLower().Replace(" ", "-") switch
        {
            "submitted" => "status-submitted",
            "in-progress" => "status-progress",
            "completed" => "status-completed",
            "released" => "status-released",
            "cancelled" => "status-cancelled",
            _ => "status-submitted"
        };
    }

    // ── 3. Test Result ───────────────────────────────────────
    public class TestResultViewModel
    {
        public string RequestID { get; set; } = "";
        public string TestName { get; set; } = "";
        public string ResultValue { get; set; } = "";
        public string Unit { get; set; } = "";
        public double NormalMin { get; set; }
        public double NormalMax { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime ResultDate { get; set; }
        public string Category { get; set; } = "";

        // e.g. "4.0 – 11.0 x10³/µL"
        public string NormalRange => $"{NormalMin} – {NormalMax} {Unit}";
    }

    // ── 4. Medical History ───────────────────────────────────
    public class MedicalHistoryViewModel
    {
        public List<string> Conditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medication { get; set; } = new();
    }

    // ── 5. Consent ───────────────────────────────────────────
    public class ConsentViewModel
    {
        public string DoctorName { get; set; } = "";
        public string HPCSANumber { get; set; } = "";
        public bool AccessGranted { get; set; }
        public DateTime? GrantedDate { get; set; }
    }

    // ── 6. Reports ───────────────────────────────────────────
    public class ReportViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        // Populated after POST filter
        public List<TestResultViewModel> FilteredResults { get; set; } = new();
    }

    // ── 7. Dashboard (combines everything) ───────────────────
    public class DashboardViewModel
    {
        public PatientProfileViewModel Profile { get; set; } = new();
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ResultsReady { get; set; }
        public int AbnormalCount { get; set; }
        public List<TestRequestViewModel> RecentRequests { get; set; } = new();
    }
}