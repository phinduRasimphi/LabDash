using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LabDash.Areas.Identity.Data;

public class LabDbContext : IdentityDbContext<LabUser>
{
    public LabDbContext(DbContextOptions<LabDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestRequest> TestRequests { get; set; }

    public DbSet<SampleReceive> SampleReceives { get; set; }

    public DbSet<TestRequestItem> TestRequestItems { get; set; }

    public DbSet<TechnicianTestType> TechnicianTestTypes { get; set; }

    public DbSet<TestResult> TestResults { get; set; }

    public DbSet<Patient> Patients { get; set; }

    public DbSet<TestType> TestTypes { get; set; }

    public DbSet<TestTypeConsumable> TestTypeConsumables { get; set; }

    public DbSet<TestVerification> TestVerifications { get; set; }

    public DbSet<TechnicianAssignment> TechnicianAssignments { get; set; }

    public DbSet<MedicalCondition> MedicalConditions { get; set; }

    public DbSet<Allergy> Allergies { get; set; }

    public DbSet<Medication> Medications { get; set; }

    public DbSet<Sample> Samples { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<SampleTypeLookup> SampleTypeLookups { get; set; }

    public DbSet<Unit> Units { get; set; }

    public DbSet<TestCategory> TestCategories { get; set; }

    public DbSet<Consumable> Consumables { get; set; }

    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<ConsumableOrder> ConsumableOrders { get; set; }

    public DbSet<PatientDoctorConsent> PatientDoctorConsents { get; set; }

    public DbSet<ConsentRequestAccess> ConsentRequestAccess { get; set; }

    // NEW — item-level consent access (replaces ConsentRequestAccess going forward)
    public DbSet<ConsentItemAccess> ConsentItemAccesses { get; set; }

    public DbSet<PatientAllergy> PatientAllergies { get; set; }

    public DbSet<PatientMedication> PatientMedications { get; set; }

    public DbSet<PatientMedicalCondition> PatientMedicalConditions { get; set; }

    public DbSet<ConsumableOrderItem> ConsumableOrderItems { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        // ============================================================
        // LAB USER
        // ============================================================

        builder.Entity<LabUser>()
            .HasIndex(u => u.HPCSANumber)
            .IsUnique()
            .HasFilter("[HPCSANumber] IS NOT NULL AND [HPCSANumber] <> ''");


        builder.Entity<LabUser>()
            .HasIndex(u => u.EmployeeNumber)
            .IsUnique()
            .HasFilter("[EmployeeNumber] IS NOT NULL AND [EmployeeNumber] <> ''");


        // ============================================================
        // PATIENT
        // ============================================================

        builder.Entity<Patient>()
            .HasOne<LabUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // CONSENT REQUEST ACCESS (legacy)
        // ============================================================

        builder.Entity<ConsentRequestAccess>()
            .HasOne(a => a.TestRequest)
            .WithMany()
            .HasForeignKey(a => a.RequestID)
            .OnDelete(DeleteBehavior.Restrict);


        // ============================================================
        // CONSENT ITEM ACCESS (new — item-level consent)
        // ============================================================

        builder.Entity<ConsentItemAccess>()
            .HasOne(a => a.Consent)
            .WithMany(c => c.ConsentItemAccesses)
            .HasForeignKey(a => a.ConsentID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ConsentItemAccess>()
            .HasOne(a => a.TestRequestItem)
            .WithMany()
            .HasForeignKey(a => a.TestRequestItemID)
            .OnDelete(DeleteBehavior.Restrict);

        // A doctor can only be granted a given item once per consent.
        builder.Entity<ConsentItemAccess>()
            .HasIndex(a => new { a.ConsentID, a.TestRequestItemID })
            .IsUnique();


        // ============================================================
        // SAMPLE RECEIVE
        // ============================================================

        builder.Entity<SampleReceive>()
            .HasOne(s => s.TestRequest)
            .WithMany(t => t.SampleReceives)
            .HasForeignKey(s => s.RequestId)
            .OnDelete(DeleteBehavior.Cascade);


        // ============================================================
        // TEST REQUEST ITEM
        // ============================================================

        builder.Entity<TestRequestItem>()
            .HasOne(t => t.TestRequest)
            .WithMany(r => r.TestRequestItems)
            .HasForeignKey(t => t.RequestId)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // TECHNICIAN TEST TYPE
        // ============================================================

        builder.Entity<TechnicianTestType>()
            .HasOne(t => t.TestType)
            .WithMany(tt => tt.TechnicianTestTypes)
            .HasForeignKey(t => t.TestTypeId)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // TEST RESULT
        // ============================================================

        builder.Entity<TestResult>()
            .HasOne(r => r.TestRequestItem)
            .WithMany()
            .HasForeignKey(r => r.TestRequestItemId)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // TEST REQUEST
        // ============================================================

        builder.Entity<TestRequest>()
            .HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // TEST TYPE CONSUMABLE
        // ============================================================

        builder.Entity<TestTypeConsumable>()
            .HasOne(x => x.TestType)
            .WithMany(x => x.TestTypeConsumables)
            .HasForeignKey(x => x.TestTypeId)
            .OnDelete(DeleteBehavior.NoAction);


        builder.Entity<TestTypeConsumable>()
            .HasOne(x => x.Consumable)
            .WithMany(x => x.TestTypeConsumables)
            .HasForeignKey(x => x.ConsumableId)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // TEST VERIFICATION
        // ============================================================

        builder.Entity<TestVerification>()
            .HasOne(v => v.TestRequestItem)
            .WithMany()
            .HasForeignKey(v => v.TestRequestItemId)
            .OnDelete(DeleteBehavior.NoAction);


        // ============================================================
        // TEST TYPE REFERENCE RANGES
        // ============================================================

        builder.Entity<TestType>()
            .Property(x => x.ReferenceRangeLow)
            .HasPrecision(18, 2);

        builder.Entity<TestType>()
            .Property(x => x.ReferenceRangeHigh)
            .HasPrecision(18, 2);


        // ============================================================
        // ADMIN SUBSYSTEM
        // ============================================================


        // ============================================================
        // ALLERGY
        // ============================================================

        builder.Entity<Allergy>()
            .Property(x => x.AllergyName)
            .HasMaxLength(100);


        builder.Entity<Allergy>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Allergies)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);


        // ============================================================
        // MEDICATION
        // ============================================================

        builder.Entity<Medication>()
            .Property(x => x.MedicationName)
            .HasMaxLength(100);


        // IMPORTANT:
        // Medication.Category is a navigation property.
        // CategoryId is the actual foreign key stored in the database.

        builder.Entity<Medication>()
            .HasOne(m => m.Category)
            .WithMany(c => c.Medications)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);


        // ============================================================
        // MEDICAL CONDITION
        // ============================================================

        builder.Entity<MedicalCondition>()
            .HasOne(m => m.Category)
            .WithMany(c => c.MedicalConditions)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);


        // ============================================================
        // AUDIT LOG
        // ============================================================

        builder.Entity<AuditLog>()
            .Property(x => x.UserName)
            .HasMaxLength(100);

        builder.Entity<AuditLog>()
            .Property(x => x.Action)
            .HasMaxLength(50);

        builder.Entity<AuditLog>()
            .Property(x => x.TableName)
            .HasMaxLength(50);
    }
}