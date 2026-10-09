using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            builder.HasIndex(a => a.Status);

            // One application per job. A job with an application can't be deleted (dismiss it instead).
            builder.HasOne(a => a.JobPosting)
                .WithOne(j => j.Application)
                .HasForeignKey<Application>(a => a.JobPostingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
