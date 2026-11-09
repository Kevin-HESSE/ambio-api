using Ambio.Api.Models;
using Ambio.Domain.JobApplications;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.Data;

public sealed class AmbioDbContext : DbContext
{
    public AmbioDbContext(DbContextOptions<AmbioDbContext> options) : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AmbioDbContext).Assembly);
    }
}
