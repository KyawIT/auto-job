using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class GeneratedTextConfiguration : IEntityTypeConfiguration<GeneratedText>
    {
        public void Configure(EntityTypeBuilder<GeneratedText> builder)
        {
            // Fast lookup of "the current cover letter of application X".
            builder.HasIndex(t => new { t.ApplicationId, t.Kind, t.IsCurrent });
        }
    }
}
