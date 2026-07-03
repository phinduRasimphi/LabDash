using LabDash.Areas.Identity.Data;
using LabDash.Models;
using Microsoft.AspNetCore.Identity;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
