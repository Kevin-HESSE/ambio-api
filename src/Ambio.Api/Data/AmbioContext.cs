using Ambio.Api.Models;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Api.Data;

public class AmbioContext : DbContext
{
    public AmbioContext(DbContextOptions<AmbioContext> options) : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
}
