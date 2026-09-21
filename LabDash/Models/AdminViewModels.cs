using LabDash.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        // === EXISTING ADMIN PROPERTIES ===
        public int ConditionCount { get; set; }
        public int AllergyCount { get; set; }
        public int MedicationCount { get; set; }
        public int UserCount { get; set; }
        public List<AuditLog> RecentChanges { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> ActivityLabels { get; set; } = new();
        public List<int> ActivityCounts { get; set; } = new();
        public Dictionary<string, int> ActivityByAction { get; set; } = new();
        public List<MedicalCondition> RecentConditions { get; set; } = new();
        public List<Medication> RecentMedications { get; set; } = new();

        // === = PATIENT PROPERTIES — NOW INSIDE THE CLASS! ===
        public PatientProfileViewModel PatientProfile { get; set; } = new();
        public int PatientInProgressRequests { get; set; }
        public int PatientTotalRequests { get; set; }
        public int PatientPendingRequests { get; set; }
        public int PatientResultsReady { get; set; }
        public int PatientAbnormalCount { get; set; }
        public List<TestRequestViewModel> PatientRecentRequests { get; set; } = new();
    }

    public class AdminListViewModel
    {
        public string PageTitle { get; set; } = "";
        public List<Category> Categories { get; set; } = new();
        public List<MedicalCondition> Conditions { get; set; } = new();
        public List<MedicalCondition> InactiveConditions { get; set; } = new();
        public string NewName { get; set; } = "";
        public int NewCategory { get; set; }
        public string NewDescription { get; set; } = "";
        public List<Category> InactiveCategories { get; set; } = new();
    }

    public class AllergyListViewModel
    {
        public string PageTitle { get; set; } = "";
        public List<Category> Categories { get; set; } = new();
        public List<Category> InactiveCategories { get; set; } = new();
        public List<Allergy> Allergies { get; set; } = new();
        public List<Allergy> InactiveAllergies { get; set; } = new();
    }


    public class MedicationListViewModel
    {
        public string PageTitle { get; set; } = "";
        public List<Category> Categories { get; set; } = new();
        public List<Category> InactiveCategories { get; set; } = new();
        public List<Medication> Medications { get; set; } = new();
        public List<Medication> InactiveMedications { get; set; } = new();
    }
    public class SystemTablesViewModel
    {
        public List<SampleTypeLookup> SampleTypes { get; set; } = new();
        public List<Unit> Units { get; set; } = new();
        public List<SampleTypeLookup> InactiveSampleTypes { get; set; } = new();
        public List<Unit> InactiveUnits { get; set; } = new();
    }

    public class AuditLogViewModel
    {
        public List<AuditEntry> Entries { get; set; } = new();
    }

    public class AdminProfileViewModel
    {
        // LabUser's PK is a string (IdentityUser default), not an int.
        public string Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(50)]
        public string Surname { get; set; }

        // Locked - display only. Maps to LabUser.SouthAfricanID.
        [Display(Name = "SA ID Number")]
        public string IDNumber { get; set; }

        // Locked - display only. Comes from the user's Identity role, not LabUser.
        public string Role { get; set; }

        [Required(ErrorMessage = "E-mail is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Cellphone is required.")]
        [RegularExpression(@"^1?[0-9]{10}$", ErrorMessage = "Enter a valid phone number.")]
        public string Cellphone { get; set; }

        // ---- ADDRESS SPLIT OVER TWO TEXT BOXES ----
        [Display(Name = "Address Line 1")]
        [StringLength(100)]
        public string AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [StringLength(100)]
        public string AddressLine2 { get; set; }

        // Helpers used by the view
        public string FullName => $"{Name} {Surname}".Trim();

        public string Initials =>
            $"{(string.IsNullOrWhiteSpace(Name) ? "" : Name.Substring(0, 1))}" +
            $"{(string.IsNullOrWhiteSpace(Surname) ? "" : Surname.Substring(0, 1))}".ToUpper();
    }
}
