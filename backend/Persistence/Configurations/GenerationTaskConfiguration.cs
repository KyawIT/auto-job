using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class GenerationTaskConfiguration : IEntityTypeConfiguration<GenerationTask>
    {
        public void Configure(EntityTypeBuilder<GenerationTask> builder)
        {
            // The worker polls for queued tasks.
            builder.HasIndex(t => t.Status);
        }
    }
}
