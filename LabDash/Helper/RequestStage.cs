namespace LabDash.Helper
{
    public static class RequestStage
    {
        public static string KeyOf(string? status)
        {
            var s = (status ?? "")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Trim()
                .ToLowerInvariant();

            if (s.Length == 0) return "submitted";

            if (s.Contains("cancel")) return "cancelled";
            if (s.Contains("release")) return "released";
            if (s.Contains("inprogress")) return "inprogress";

            if (s.Contains("submitted") || s.Contains("pending") || s.Contains("received"))
                return "submitted";

            return "completed";
        }

        public static int StepOf(string? status) => KeyOf(status) switch
        {
            "submitted" => 1,
            "inprogress" => 2,
            "completed" => 3,
            "released" => 4,
            _ => 0
        };

        public static bool IsReleased(string? status) => KeyOf(status) == "released";

        public static bool IsCancelled(string? status) => KeyOf(status) == "cancelled";
    }
}