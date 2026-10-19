// =====================================================================
// Services/HealthEntryService.cs
//
// Saves the "add your own" conditions, allergies and medication that a
// patient types in, using the same rules as PatientController.Profile:
//   - matched by name, so nothing is duplicated
//   - every field except Name is optional
//   - a category is resolved (picked, typed, or "Other")
//
// It does NOT call SaveChanges. The caller does, so everything is
// saved in one go.
//
// Adjust the usings if LabDbContext / NewHealthEntry live in a
// different namespace in your project.
// =====================================================================

using System.Globalization;
using LabDash.Areas.Identity.Data;
using LabDash.Models;
using LabDash.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Services
{
    public class HealthEntryService
    {
        private readonly LabDbContext _context;

        public HealthEntryService(LabDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------
        // helpers (public so Register can build the text columns too)
        // ------------------------------------------------------------

        public static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            return DateTime.TryParseExact(
                s.Trim(),
                new[] { "yyyy-MM", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var d)
                ? d
                : null;
        }

        private static string? Clean(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // Names are limited to 100 characters in the database.
        public static List<NewHealthEntry> UniqueNamed(List<NewHealthEntry>? list) =>
            (list ?? new List<NewHealthEntry>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x =>
                {
                    var n = x.Name!.Trim();
                    x.Name = n.Length > 100 ? n.Substring(0, 100) : n;
                    return x;
                })
                .GroupBy(x => x.Name!.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

        private async Task<Category> ResolveCategoryAsync(
            NewHealthEntry e,
            string type,
            Dictionary<string, Category> cache)
        {
            if (e.CategoryId.HasValue)
            {
                var chosen = await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.CategoryId == e.CategoryId.Value &&
                        c.Type == type);

                if (chosen != null) return chosen;
            }

            var name = string.IsNullOrWhiteSpace(e.NewCategoryName)
                ? "Other"
                : e.NewCategoryName.Trim();

            if (name.Length > 100) name = name.Substring(0, 100);

            var key = type + "|" + name.ToLowerInvariant();

            if (cache.TryGetValue(key, out var hit)) return hit;

            var cat = await _context.Categories
                .FirstOrDefaultAsync(c => c.Type == type && c.Name == name);

            if (cat == null)
            {
                cat = new Category { Name = name, Type = type, IsActive = true };
                _context.Categories.Add(cat);
            }

            cache[key] = cat;

            return cat;
        }

        // ------------------------------------------------------------
        // 1) Link the items ticked in the lists (idempotent: only adds
        //    rows that are missing, so it is safe even if Register
        //    already links them).
        // ------------------------------------------------------------

        public async Task LinkSelectedAsync(
            int patientId,
            List<int>? conditionIds,
            List<int>? allergyIds,
            List<int>? medicationIds)
        {
            if (conditionIds != null && conditionIds.Any())
            {
                var valid = await _context.MedicalConditions
                    .Where(x => x.IsActive && conditionIds.Contains(x.MedicalConditionId))
                    .Select(x => x.MedicalConditionId)
                    .ToListAsync();

                var have = await _context.PatientMedicalConditions
                    .Where(x => x.PatientID == patientId)
                    .Select(x => x.MedicalConditionId)
                    .ToListAsync();

                foreach (var id in valid.Except(have))
                {
                    _context.PatientMedicalConditions.Add(new PatientMedicalCondition
                    {
                        PatientID = patientId,
                        MedicalConditionId = id
                    });
                }
            }

            if (allergyIds != null && allergyIds.Any())
            {
                var valid = await _context.Allergies
                    .Where(x => x.IsActive && allergyIds.Contains(x.AllergyId))
                    .Select(x => x.AllergyId)
                    .ToListAsync();

                var have = await _context.PatientAllergies
                    .Where(x => x.PatientID == patientId)
                    .Select(x => x.AllergyId)
                    .ToListAsync();

                foreach (var id in valid.Except(have))
                {
                    _context.PatientAllergies.Add(new PatientAllergy
                    {
                        PatientID = patientId,
                        AllergyId = id,
                        RecordedDate = DateTime.Today
                    });
                }
            }

            if (medicationIds != null && medicationIds.Any())
            {
                var valid = await _context.Medications
                    .Where(x => x.IsActive && medicationIds.Contains(x.MedicationId))
                    .Select(x => x.MedicationId)
                    .ToListAsync();

                var have = await _context.PatientMedications
                    .Where(x => x.PatientID == patientId)
                    .Select(x => x.MedicationId)
                    .ToListAsync();

                foreach (var id in valid.Except(have))
                {
                    _context.PatientMedications.Add(new PatientMedication
                    {
                        PatientID = patientId,
                        MedicationId = id
                    });
                }
            }
        }

        // ------------------------------------------------------------
        // 2) Add the entries the patient typed in themselves
        // ------------------------------------------------------------

        public async Task AddNewEntriesAsync(
            int patientId,
            List<NewHealthEntry>? medications,
            List<NewHealthEntry>? allergies,
            List<NewHealthEntry>? conditions)
        {
            var cache = new Dictionary<string, Category>();

            // ---------- MEDICATIONS ----------
            foreach (var e in UniqueNamed(medications))
            {
                var name = e.Name!;

                var med = await _context.Medications
                    .FirstOrDefaultAsync(m => m.MedicationName == name);

                if (med == null)
                {
                    med = new Medication
                    {
                        MedicationName = name,
                        Description = name,
                        IsActive = true,
                        Category = await ResolveCategoryAsync(e, "Medication", cache)
                    };

                    _context.Medications.Add(med);
                }

                var link =
                    _context.PatientMedications.Local.FirstOrDefault(x =>
                        x.PatientID == patientId && x.Medication == med)
                    ?? (med.MedicationId != 0
                        ? await _context.PatientMedications.FirstOrDefaultAsync(x =>
                            x.PatientID == patientId &&
                            x.MedicationId == med.MedicationId)
                        : null);

                if (link == null)
                {
                    _context.PatientMedications.Add(new PatientMedication
                    {
                        PatientID = patientId,
                        Medication = med,
                        Dosage = Clean(e.Dosage),
                        Frequency = Clean(e.Frequency),
                        StartDate = ParseDate(e.StartDate),
                        EndDate = ParseDate(e.EndDate),
                        Notes = Clean(e.Notes)
                    });
                }
                else
                {
                    link.Dosage = Clean(e.Dosage) ?? link.Dosage;
                    link.Frequency = Clean(e.Frequency) ?? link.Frequency;
                    link.StartDate = ParseDate(e.StartDate) ?? link.StartDate;
                    link.EndDate = ParseDate(e.EndDate) ?? link.EndDate;
                    link.Notes = Clean(e.Notes) ?? link.Notes;
                }
            }

            // ---------- ALLERGIES ----------
            foreach (var e in UniqueNamed(allergies))
            {
                var name = e.Name!;

                var allergy = await _context.Allergies
                    .FirstOrDefaultAsync(a => a.AllergyName == name);

                if (allergy == null)
                {
                    allergy = new Allergy
                    {
                        AllergyName = name,
                        Description = name,
                        IsActive = true,
                        Category = await ResolveCategoryAsync(e, "Allergy", cache)
                    };

                    _context.Allergies.Add(allergy);
                }

                var link =
                    _context.PatientAllergies.Local.FirstOrDefault(x =>
                        x.PatientID == patientId && x.Allergy == allergy)
                    ?? (allergy.AllergyId != 0
                        ? await _context.PatientAllergies.FirstOrDefaultAsync(x =>
                            x.PatientID == patientId &&
                            x.AllergyId == allergy.AllergyId)
                        : null);

                if (link == null)
                {
                    _context.PatientAllergies.Add(new PatientAllergy
                    {
                        PatientID = patientId,
                        Allergy = allergy,
                        Severity = Clean(e.Severity),
                        RecordedDate = ParseDate(e.StartDate) ?? DateTime.Today,
                        Notes = Clean(e.Notes)
                    });
                }
                else
                {
                    link.Severity = Clean(e.Severity) ?? link.Severity;
                    link.Notes = Clean(e.Notes) ?? link.Notes;
                }
            }

            // ---------- CONDITIONS ----------
            foreach (var e in UniqueNamed(conditions))
            {
                var name = e.Name!;

                var cond = await _context.MedicalConditions
                    .FirstOrDefaultAsync(c => c.ConditionName == name);

                if (cond == null)
                {
                    cond = new MedicalCondition
                    {
                        ConditionName = name,
                        Description = name,
                        IsActive = true,
                        Category = await ResolveCategoryAsync(e, "MedicalCondition", cache)
                    };

                    _context.MedicalConditions.Add(cond);
                }

                var link =
                    _context.PatientMedicalConditions.Local.FirstOrDefault(x =>
                        x.PatientID == patientId && x.MedicalCondition == cond)
                    ?? (cond.MedicalConditionId != 0
                        ? await _context.PatientMedicalConditions.FirstOrDefaultAsync(x =>
                            x.PatientID == patientId &&
                            x.MedicalConditionId == cond.MedicalConditionId)
                        : null);

                if (link == null)
                {
                    _context.PatientMedicalConditions.Add(new PatientMedicalCondition
                    {
                        PatientID = patientId,
                        MedicalCondition = cond,
                        Severity = Clean(e.Severity),
                        DiagnosisDate = ParseDate(e.StartDate),
                        Notes = Clean(e.Notes)
                    });
                }
                else
                {
                    link.Severity = Clean(e.Severity) ?? link.Severity;
                    link.DiagnosisDate = ParseDate(e.StartDate) ?? link.DiagnosisDate;
                    link.Notes = Clean(e.Notes) ?? link.Notes;
                }
            }
        }
    }
}