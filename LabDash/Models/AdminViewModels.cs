using System.Collections.Generic;

namespace LabDash.Models
{
    public class AuditEntry
    {
        public string Timestamp { get; set; } = "";
        public string User { get; set; } = "";
        public string Role { get; set; } = "";
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
    }

    public class SystemTableItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class AdminDashboardViewModel
    {
        public int ConditionCount { get; set; }
        public int AllergyCount { get; set; }
        public int MedicationCount { get; set; }
        public int UserCount { get; set; }

        public List<MedicalCondition> RecentConditions { get; set; } = new();
        public List<Medication> RecentMedications { get; set; } = new();
    }

    public class AdminListViewModel
    {
        public string PageTitle { get; set; } = "";

        public List<Category> Categories { get; set; } = new();

        // Active Conditions
        public List<MedicalCondition> Conditions { get; set; } = new();

        // Soft Deleted Conditions
        public List<MedicalCondition> InactiveConditions { get; set; } = new();

        public string NewName { get; set; } = "";
        public int NewCategory { get; set; }
        public string NewDescription { get; set; } = "";
    }

    public class AllergyListViewModel
    {
        public string PageTitle { get; set; } = "";

        public List<string> Categories { get; set; } = new();

        public List<Allergy> Allergies { get; set; } = new();

        public List<Allergy> InactiveAllergies { get; set; } = new();

        public string NewName { get; set; } = "";
        public string NewCategory { get; set; } = "";
        public string NewDescription { get; set; } = "";
    }

    public class MedicationListViewModel
    {
        public string PageTitle { get; set; } = "";

        public List<string> Categories { get; set; } = new();

        public List<Medication> Medications { get; set; } = new();

        public List<Medication> InactiveMedications { get; set; } = new();

        public string NewName { get; set; } = "";
        public string NewCategory { get; set; } = "";
        public string NewDescription { get; set; } = "";
    }

    public class SystemTablesViewModel
    {
        public List<SampleTypeLookup> SampleTypes { get; set; } = new();
        public List<Unit> Units { get; set; } = new();
    }

    public class AuditLogViewModel
    {
        public List<AuditEntry> Entries { get; set; } = new();
    }

    



}