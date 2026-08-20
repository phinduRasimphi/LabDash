using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        builder.Entity<Patient>()
        .HasOne<LabUser>()
        .WithMany()
        .HasForeignKey(p => p.UserId)
        .HasPrincipalKey(u => u.Id)
        .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<SampleReceive>()
            .HasOne(s => s.TestRequest)
            .WithMany(t => t.SampleReceives)
            .HasForeignKey(s => s.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TestRequestItem>()
            .HasOne(t => t.TestRequest)
            .WithMany(r => r.TestRequestItems)
            .HasForeignKey(t => t.RequestId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TechnicianTestType>()
            .HasOne(t => t.TestType)
            .WithMany(tt => tt.TechnicianTestTypes)
            .HasForeignKey(t => t.TestTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TestResult>()
            .HasOne(r => r.TestRequestItem)
            .WithMany()
            .HasForeignKey(r => r.TestRequestItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TestRequest>()
            .HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.NoAction);

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

        builder.Entity<TestVerification>()
            .HasOne(v => v.TestRequestItem)
            .WithMany()
            .HasForeignKey(v => v.TestRequestItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // =============================
        // Admin Subsystem Configuration
        // =============================





        builder.Entity<Allergy>()
            .Property(x => x.AllergyName)
            .HasMaxLength(100);

        builder.Entity<Allergy>()
            .Property(x => x.Category)
            .HasMaxLength(50);

        builder.Entity<Medication>()
            .Property(x => x.MedicationName)
            .HasMaxLength(100);

        builder.Entity<Medication>()
            .Property(x => x.Category)
            .HasMaxLength(50);

        builder.Entity<AuditLog>()
            .Property(x => x.UserName)
            .HasMaxLength(100);

        builder.Entity<AuditLog>()
            .Property(x => x.Action)
            .HasMaxLength(50);

        builder.Entity<AuditLog>()
            .Property(x => x.TableName)
            .HasMaxLength(50);

        builder.Entity<MedicalCondition>()
    .HasOne(m => m.Category)
    .WithMany()
    .HasForeignKey(m => m.CategoryId)
    .OnDelete(DeleteBehavior.Restrict); // categories are soft-deleted, never hard-deleted
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        
    }
}
