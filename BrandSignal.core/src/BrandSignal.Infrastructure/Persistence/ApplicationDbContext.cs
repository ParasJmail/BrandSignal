using BrandSignal.Application.Common.Interfaces;
using BrandSignal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrandSignal.Infrastructure.Persistence;

// The ":" means this class extends EF Core's master DbContext 
// AND signs our custom interface contract.
public class ApplicationDbContext : DbContext, IApplicationDbContext{

    // This constructor intercepts database settings (like server URL and passwords)
    // when the application boots up.
    // The DbContextOptions parameter acts as a Configuration Capsule [A data packet carrying setup info like connection strings, passwords, and server locations]. When our API layer boots up, it reads our secure application settings file, creates this options capsule containing our SQL Server connection details, and injects it right here. The : base(options) part instantly forwards that capsule straight up to Microsoft’s master database engine so it knows exactly where the server lives on the network.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){

    }

    // This creates the direct link between our C# 'Campaign' class 
    // and a physical table inside SQL Server called 'Campaigns'.
    // Technical Term: DbSet Property with Expression Body Syntax [A special C# mapping property that bridges a C# object class directly to a physical database table layout].
    // In Full Detail: DbSet<Campaign> represents a live, digital data sheet of our Campaign records. This line acts as a direct Object-Relational Mapping (ORM) [A software translator bridge that allows developers to interact with a database using clean C# objects instead of writing raw, messy SQL strings manually]. By writing this line, we tell Entity Framework Core: "Look at our Domain Campaign.cs file. Go look into our SQL Server database, find a table named Campaigns, and completely handle translating data back and forth between them."
    public DbSet<Campaign> Campaigns => Set<Campaign>();

    // New DbSets for AuditReport, AuditCompetitor, and AuditRecommendedKeyword entities
    public DbSet<AuditReport> AuditReports => Set<AuditReport>();
    public DbSet<AuditCompetitor> AuditCompetitors => Set<AuditCompetitor>();
    public DbSet<AuditRecommendedKeyword> AuditRecommendedKeywords => Set<AuditRecommendedKeyword>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure AuditReport relationship and cascade delete behaviour
        modelBuilder.Entity<AuditReport>(builder =>
        {
            builder.HasKey(a => a.Id);

            // Campaign (1) ---> AuditReport (Many)
            builder.HasOne(a => a.Campaign)
                .WithMany(c => c.AuditReports)
                .HasForeignKey(a => a.CampaignId)
                .OnDelete(DeleteBehavior.Cascade); // When a Campaign is deleted, its related AuditReports will also be deleted
            
            // AuditReport (1) ---> AuditCompetitor (Many)
            builder.HasMany(a => a.Competitors)
                .WithOne(c => c.AuditReport)
                .HasForeignKey(c => c.AuditReportId)
                .OnDelete(DeleteBehavior.Cascade); // When an AuditReport is deleted, its related AuditCompetitors will also be deleted

            // AuditReport (1) ---> AuditRecommendedKeyword (Many)
            builder.HasMany(a => a.RecommendedKeywords)
                .WithOne(k => k.AuditReport)
                .HasForeignKey(k => k.AuditReportId)
                .OnDelete(DeleteBehavior.Cascade); // When an AuditReport is deleted, its related AuditRecommendedKeywords will also be deleted
        });
    }

    // This fulfills our contract's promise to provide a Save button.
    // It captures any changes sitting in the computer memory and writes them to the disk.
    // Our interface contract promised that the Application layer would get a working "Save" button to commit changes to the hard drive. By writing public override Task<int> SaveChangesAsync, we are explicitly hooking up that interface button to Microsoft's underlying background file system engine.
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default){
        // When this line runs, Entity Framework Core scans the local memory to see what the manager added (like our new campaign record). It immediately translates that C# data into raw database dialect (INSERT INTO [Campaigns] (Id, CompanyName...) VALUES (...)) and fires a secure network execution command straight into the SQL Server instance. The cancellationToken [A safety kill-switch that cancels the database save instantly if the user closes their browser or cancels the action] makes sure the physical server hardware never gets hung up on dead connections.
        return base.SaveChangesAsync(cancellationToken);
    }
}