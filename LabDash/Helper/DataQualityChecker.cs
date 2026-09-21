using LabDash.Areas.Identity.Data;
using LabDash.Models;

namespace LabDash.Helpers
{
    public record DataQualityItem(string Text, string Action);

    public class DataQualityCheck
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Summary { get; set; } = "";
        public bool Advisory { get; set; }          // suggestions don't count as warnings on the dashboard
        public List<DataQualityItem> Items { get; set; } = new();
        public int Count => Items.Count;
        public bool Passed => Items.Count == 0;
    }

    public static class DataQualityChecker
    {
        public static List<DataQualityCheck> Run(LabDbContext context) => new()
        {
            EmptyTables(context),
            DuplicateNames(context),
            ArchivedCategories(context),
            EmptyCategories(context)
        };

        private static DataQualityCheck EmptyTables(LabDbContext context)
        {
            var check = new DataQualityCheck
            {
                Title = "Empty reference tables",
                Description = "Tables with no active records, so there is nothing to choose from."
            };

            if (!context.MedicalConditions.Any(x => x.IsActive))
                check.Items.Add(new DataQualityItem("There are no active conditions", "Conditions"));
            if (!context.Allergies.Any(x => x.IsActive))
                check.Items.Add(new DataQualityItem("There are no active allergies", "Allergies"));
            if (!context.Medications.Any(x => x.IsActive))
                check.Items.Add(new DataQualityItem("There are no active medications", "Medications"));
            if (!context.SampleTypeLookups.Any(x => x.IsActive))
                check.Items.Add(new DataQualityItem("There are no active sample types", "SystemTables"));
            if (!context.Units.Any(x => x.IsActive))
                check.Items.Add(new DataQualityItem("There are no active units", "SystemTables"));

            check.Summary = $"{check.Count} reference table(s) have no active records.";
            return check;
        }

        private static DataQualityCheck DuplicateNames(LabDbContext context)
        {
            var check = new DataQualityCheck
            {
                Title = "Duplicate names",
                Description = "Active records in the same table that share a name (ignoring capital letters and extra spaces)."
            };

            AddDuplicates(check, "Condition", "Conditions",
                context.MedicalConditions.Where(x => x.IsActive).Select(x => x.ConditionName).ToList());
            AddDuplicates(check, "Allergy", "Allergies",
                context.Allergies.Where(x => x.IsActive).Select(x => x.AllergyName).ToList());
            AddDuplicates(check, "Medication", "Medications",
                context.Medications.Where(x => x.IsActive).Select(x => x.MedicationName).ToList());
            AddDuplicates(check, "Sample type", "SystemTables",
                context.SampleTypeLookups.Where(x => x.IsActive).Select(x => x.Name).ToList());
            AddDuplicates(check, "Unit", "SystemTables",
                context.Units.Where(x => x.IsActive).Select(x => x.Name).ToList());

            check.Summary = $"{check.Count} duplicated name(s) found.";
            return check;
        }

        private static void AddDuplicates(DataQualityCheck check, string label, string action, IEnumerable<string?> names)
        {
            var groups = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .GroupBy(n => n!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var g in groups)
                check.Items.Add(new DataQualityItem($"{label} '{g.Key}' appears {g.Count()} times", action));
        }

        private static DataQualityCheck ArchivedCategories(LabDbContext context)
        {
            var check = new DataQualityCheck
            {
                Title = "Active records in archived categories",
                Description = "Records that are still active but belong to a category that has been archived."
            };

            foreach (var row in context.MedicalConditions
                .Where(m => m.IsActive && m.Category != null && !m.Category.IsActive)
                .Select(m => new { Name = m.ConditionName, Category = m.Category!.Name }).ToList())
                check.Items.Add(new DataQualityItem($"Condition '{row.Name}' is in the archived category '{row.Category}'", "Conditions"));

            foreach (var row in context.Allergies
                .Where(m => m.IsActive && m.Category != null && !m.Category.IsActive)
                .Select(m => new { Name = m.AllergyName, Category = m.Category!.Name }).ToList())
                check.Items.Add(new DataQualityItem($"Allergy '{row.Name}' is in the archived category '{row.Category}'", "Allergies"));

            foreach (var row in context.Medications
                .Where(m => m.IsActive && m.Category != null && !m.Category.IsActive)
                .Select(m => new { Name = m.MedicationName, Category = m.Category!.Name }).ToList())
                check.Items.Add(new DataQualityItem($"Medication '{row.Name}' is in the archived category '{row.Category}'", "Medications"));

            check.Summary = $"{check.Count} active record(s) belong to an archived category.";
            return check;
        }

        private static DataQualityCheck EmptyCategories(LabDbContext context)
        {
            var check = new DataQualityCheck
            {
                Title = "Unused categories",
                Description = "Active categories that no active record uses. Add records to them or archive them.",
                Advisory = true
            };

            AddEmptyCategories(check, context, "MedicalCondition", "Condition", "Conditions",
                context.MedicalConditions.Where(x => x.IsActive && x.Category != null).Select(x => x.Category!.Name).Distinct().ToList());
            AddEmptyCategories(check, context, "Allergy", "Allergy", "Allergies",
                context.Allergies.Where(x => x.IsActive && x.Category != null).Select(x => x.Category!.Name).Distinct().ToList());
            AddEmptyCategories(check, context, "Medication", "Medication", "Medications",
                context.Medications.Where(x => x.IsActive && x.Category != null).Select(x => x.Category!.Name).Distinct().ToList());

            check.Summary = $"{check.Count} category(ies) are not used.";
            return check;
        }

        private static void AddEmptyCategories(DataQualityCheck check, LabDbContext context,
            string type, string label, string action, List<string> usedNames)
        {
            var names = context.Categories
                .Where(c => c.Type == type && c.IsActive)
                .Select(c => c.Name)
                .ToList();

            foreach (var name in names.Where(n => !usedNames.Contains(n, StringComparer.OrdinalIgnoreCase)))
                check.Items.Add(new DataQualityItem($"{label} category '{name}' has no active records", action));
        }
    }
}