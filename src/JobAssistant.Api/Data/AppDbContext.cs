using JobAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobAssistant.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200);
            entity.Property(c => c.BoardToken).HasMaxLength(100);
            entity.Property(c => c.Board).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(c => new { c.Board, c.BoardToken }).IsUnique();

            // Starter watchlist. Add your own with POST /api/companies.
            entity.HasData(
                new Company { Id = 1, Name = "Anthropic", Board = JobBoard.Greenhouse, BoardToken = "anthropic" },
                new Company { Id = 2, Name = "Pinterest", Board = JobBoard.Greenhouse, BoardToken = "pinterest" },
                new Company { Id = 3, Name = "Palantir", Board = JobBoard.Lever, BoardToken = "palantir" });
        });

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.Property(p => p.ExternalId).HasMaxLength(100);
            entity.Property(p => p.Title).HasMaxLength(JobPosting.TitleMaxLength);
            entity.Property(p => p.Location).HasMaxLength(JobPosting.LocationMaxLength);
            entity.Property(p => p.Url).HasMaxLength(1000);

            // A posting is unique per company, so re-running ingestion never duplicates rows.
            entity.HasIndex(p => new { p.CompanyId, p.ExternalId }).IsUnique();
            entity.HasIndex(p => p.FirstSeenAt);

            entity.HasOne(p => p.Company)
                .WithMany(c => c.JobPostings)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
