using Ambio.Domain.JobApplications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambio.Data.Mapping;

public class JobApplicationMapping : IEntityTypeConfiguration<JobApplication>
{
    private const string Discriminator = "kind";

    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.Property(ja => ja.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(ja => ja.Title)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(ja => ja.Company)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(ja => ja.Status)
            .HasConversion<string>();

        builder.HasDiscriminator<string>(Discriminator)
            .HasValue<OfferApplication>(nameof(ApplicationKind.Offer))
            .HasValue<SpontaneousApplication>(nameof(ApplicationKind.Spontaneous));

        builder.HasMany(ja => ja.StatusHistory)
            .WithOne()
            .HasForeignKey(s => s.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ja => ja.StatusHistory)
            .HasField("_statusHistory");
    }
}
