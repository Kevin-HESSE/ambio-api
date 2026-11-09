using Ambio.Domain.JobApplications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambio.Api.Data.EntityMapping;

public class JobApplicationMapping : IEntityTypeConfiguration<JobApplication>
{
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

        builder.Property(ja => ja.Url)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ja => ja.Status)
            .HasConversion<string>();
    }
}
