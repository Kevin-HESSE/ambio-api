using Ambio.Domain.JobApplications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambio.Data.Mapping;

public class OfferApplicationMapping : IEntityTypeConfiguration<OfferApplication>
{
    public void Configure(EntityTypeBuilder<OfferApplication> builder)
    {
        builder.Property(o => o.Url)
            .HasMaxLength(2048);
    }
}
