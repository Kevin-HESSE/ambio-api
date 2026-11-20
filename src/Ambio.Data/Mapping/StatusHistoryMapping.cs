using Ambio.Domain.JobApplications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambio.Data.Mapping;

public class StatusHistoryMapping : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> builder)
    {
        builder.Property(s => s.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(s => s.StatusFrom)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.StatusTo)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.ChangedAt)
            .IsRequired();
    }
}
