using Ambio.Api.Models;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.Data;

public class AmbioDbContext : DbContext
{
    public AmbioDbContext(DbContextOptions<AmbioDbContext> options) : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
}
