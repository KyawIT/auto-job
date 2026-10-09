using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
    {
        public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
        {
            builder.HasKey(ad => new { ad.ApplicationId, ad.DocumentId });

            builder.HasOne(ad => ad.Application)
                .WithMany(a => a.Attachments)
                .HasForeignKey(ad => ad.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // A document still attached to an application can't be deleted.
            builder.HasOne(ad => ad.Document)
                .WithMany(d => d.Applications)
                .HasForeignKey(ad => ad.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
