using LabDash.Areas.Identity.Data;
using LabDash.Models;

namespace LabDash.Helpers
{
    public static class AdminDashboardBuilder
    {
        public static void Populate(AdminDashboardViewModel vm, LabDbContext context)
        {
            vm.ConditionCount = context.MedicalConditions.Count(x => x.IsActive);
            vm.AllergyCount = context.Allergies.Count(x => x.IsActive);
            vm.MedicationCount = context.Medications.Count(x => x.IsActive);

            vm.RecentChanges = context.AuditLogs
                .OrderByDescending(x => x.ActionDate)
                .Take(6)
                .ToList();

            // Same checks as the Data quality page (suggestions are left out of the warnings)
            vm.Warnings = DataQualityChecker.Run(context)
                .Where(c => !c.Passed && !c.Advisory)
                .Select(c => c.Summary)
                .ToList();

            // Activity over the last 30 days
            var since = DateTime.Today.AddDays(-29);

            var rows = context.AuditLogs
                .Where(x => x.ActionDate >= since)
                .Select(x => new { x.ActionDate, x.Action })
                .ToList();

            var byDay = rows
                .GroupBy(r => r.ActionDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            vm.ActivityLabels = new List<string>();
            vm.ActivityCounts = new List<int>();

            for (int i = 0; i < 30; i++)
            {
                var day = since.AddDays(i);
                vm.ActivityLabels.Add(day.ToString("dd MMM"));
                vm.ActivityCounts.Add(byDay.TryGetValue(day, out var count) ? count : 0);
            }

            // What kind of changes were made (drives the "What changed" bar)
            int CountOf(params string[] actions) =>
                rows.Count(r => actions.Contains(r.Action, StringComparer.OrdinalIgnoreCase));

            vm.ActivityByAction = new Dictionary<string, int>
            {
                ["Created"] = CountOf("Created"),
                ["Updated"] = CountOf("Updated"),
                ["Removed"] = CountOf("Deactivated", "Deleted"),
                ["Restored"] = CountOf("Reactivated")
            };
        }
    }
}