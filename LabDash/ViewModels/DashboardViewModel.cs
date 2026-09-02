namespace LabDash.ViewModels
{
   
        public class DoctorDashboardViewModel
        {
            // Statistics
            public int TotalRequests { get; set; }
            public int PendingRequests { get; set; }
            public int CompletedRequests { get; set; }
            public int ReleasedResults { get; set; }
            public int AbnormalResults { get; set; }
            public int UrgentRequests { get; set; }
            public int StatRequests { get; set; }
            public int TodayRequests { get; set; }

            // Recent Test Requests (without full patient details)
            public List<RecentRequestViewModel> RecentRequests { get; set; } = new();

            // Requests by Status (for chart)
            public Dictionary<string, int> RequestsByStatus { get; set; } = new();

            // Monthly trend
            public List<MonthlyTrendViewModel> MonthlyTrend { get; set; } = new();

            // Urgency distribution
            public Dictionary<string, int> RequestsByUrgency { get; set; } = new();

            // Alerts/Notifications
            public List<AlertViewModel> Alerts { get; set; } = new();
        }

        public class RecentRequestViewModel
        {
            public int RequestId { get; set; }
            public string PatientInitials { get; set; } // Only initials for privacy
            public string PatientId { get; set; } // Masked ID
            public string Urgency { get; set; }
            public string Status { get; set; }
            public DateTime RequestDate { get; set; }
            public int TestCount { get; set; }
            public bool HasAbnormalResults { get; set; }
            public string TimeAgo => GetTimeAgo(RequestDate);

            private string GetTimeAgo(DateTime timestamp)
            {
                var diff = DateTime.UtcNow - timestamp;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
                return timestamp.ToString("dd MMM");
            }
        }

        public class MonthlyTrendViewModel
        {
            public string Month { get; set; }
            public int Count { get; set; }
        }

        public class AlertViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public string Type { get; set; } // Warning, Info, Success, Danger
            public DateTime Timestamp { get; set; }
            public bool IsRead { get; set; }
            public string TimeAgo => GetTimeAgo(Timestamp);

            private string GetTimeAgo(DateTime timestamp)
            {
                var diff = DateTime.UtcNow - timestamp;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
                return timestamp.ToString("dd MMM");
            }
        }
    
}
