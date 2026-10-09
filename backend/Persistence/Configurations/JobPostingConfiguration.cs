using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
    {
        public void Configure(EntityTypeBuilder<JobPosting> builder)
        {
            // Refreshing a saved search must not insert the same job twice.
            builder.HasIndex(j => new { j.Source, j.ExternalId }).IsUnique();
            builder.HasIndex(j => j.State);

            // Deleting a saved search keeps the jobs it found.
            builder.HasOne(j => j.SavedSearch)
                .WithMany(s => s.JobPostings)
                .HasForeignKey(j => j.SavedSearchId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
