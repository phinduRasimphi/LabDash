// Put this file in your Models folder:  LabDash/Models/RequestBuckets.cs
namespace LabDash.Models
{
    /// <summary>
    /// The single definition of "pending / in progress / ready".
    /// The dashboard cards (HomeController.Index) and the filtered list pages
    /// (PatientController.Requests) both use this, so a card's number always
    /// matches the rows you see after clicking it.
    ///
    /// IMPORTANT: these must match the exact text stored in TestRequest.Status.
    /// If your in-progress status is spelled differently, add that spelling here.
    /// </summary>
    public static class RequestBuckets
    {
        public static readonly string[] Pending = { "Submitted" };
        public static readonly string[] InProgress = { "InProgress", "In Progress", "Completed" }; // Completed = analysed, awaiting doctor release
        public static readonly string[] Ready = { "Released" };

        // In-memory check, case-insensitive (use this on already-loaded lists).
        public static bool In(string[] bucket, string? status) =>
            bucket.Contains(status ?? "", StringComparer.OrdinalIgnoreCase);

        // Maps the ?status= value from the dashboard links to a bucket. null = no filter.
        public static string[]? For(string? key) => (key ?? "").Trim().ToLowerInvariant() switch
        {
            "pending" => Pending,
            "inprogress" => InProgress,
            "ready" => Ready,
            _ => null
        };
    }
}