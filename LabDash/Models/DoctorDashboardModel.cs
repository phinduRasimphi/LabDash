namespace LabDash.Models
{
    /// <summary>
    /// View model for the Doctor dashboard page.
    /// Everything the Doctor's dashboard needs is built here in one pass.
    /// </summary>
    public class DoctorDashboardModel
    {
        // ------------------------------------------------------------
        // Identity (welcome banner)
        // ------------------------------------------------------------
        public string DoctorFullName { get; set; } = "";
        public string DoctorInitials { get; set; } = "";

        // ------------------------------------------------------------
        // Stat cards — top row (4 numbers)
        // ------------------------------------------------------------
        public int SharedPatientCount { get; set; }
        public int SharedItemCount { get; set; }
        public int RecentConsentCount { get; set; }

        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int ReleasedRequests { get; set; }

        public int AbnormalResults { get; set; }
        public int UnreadNotificationCount { get; set; }

        // ------------------------------------------------------------
        // Recent shared patients (top 5)
        // ------------------------------------------------------------
        public List<DoctorSharedPatientItem> RecentSharedPatients { get; set; } = new();

        // ------------------------------------------------------------
        // Recent notifications (top 5) — mini-feed on dashboard
        // ------------------------------------------------------------
        public List<DoctorNotificationItem> RecentNotifications { get; set; } = new();

        // ------------------------------------------------------------
        // Chart data — results captured, last 14 days
        // ------------------------------------------------------------
        public List<string> ResultsTrendLabels { get; set; } = new();
        public List<int> ResultsTrendCounts { get; set; } = new();

        // ------------------------------------------------------------
        // Chart data — consent activity, last 30 days
        // ------------------------------------------------------------
        public List<string> ConsentTrendLabels { get; set; } = new();
        public List<int> ConsentTrendCounts { get; set; } = new();
    }

    // ------------------------------------------------------------
    // Supporting types
    // ------------------------------------------------------------

    /// <summary>
    /// One patient who has granted consent — shown in the
    /// "Recently shared with you" panel.
    /// </summary>
    public class DoctorSharedPatientItem
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = "";
        public string IDNumber { get; set; } = "";
        public int SharedItemCount { get; set; }
        public DateTime LastGrantedDate { get; set; }

        // Initials for the avatar circle (e.g. "TM" for Thabo Mokoena).
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "?";

                var parts = FullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0) return "?";
                if (parts.Length == 1) return parts[0][0].ToString().ToUpper();

                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
            }
        }
    }

    /// <summary>
    /// One row in the dashboard's notification feed.
    /// </summary>
    public class DoctorNotificationItem
    {
        public int NotificationID { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public string TimeAgo
        {
            get
            {
                var s = (int)(DateTime.Now - CreatedAt).TotalSeconds;
                if (s < 60) return s + "s ago";
                if (s < 3600) return (s / 60) + "m ago";
                if (s < 86400) return (s / 3600) + "h ago";
                return (s / 86400) + "d ago";
            }
        }
    }
}