using System.Data.Common;

using Ambio.Api.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Npgsql;

using Respawn;

using Testcontainers.PostgreSql;

namespace Ambio.Api.IntegrationTests;

public class AmbioApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public const string BaseCollectionDefinition = "IntegrationTests";
    private const string PostgresImage = "postgres:17";
    private const string ProjectName = "ambio";

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase(ProjectName)
        .WithUsername(ProjectName)
        .WithPassword(ProjectName)
        .WithCleanUp(true)
        .Build();

    private string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Disable logging for integration tests
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureServices(collection =>
        {
            collection.RemoveAll<AmbioDbContext>();
            collection.AddDbContext<AmbioDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
                // In case of pending model changes, ignore the warning
                // options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });
        });
    }

    public async ValueTask ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AmbioDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        _dbConnection = new NpgsqlConnection(ConnectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbConnection.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition(AmbioApiFactory.BaseCollectionDefinition)]
public class IntegrationTestCollection : ICollectionFixture<AmbioApiFactory>
{
}
