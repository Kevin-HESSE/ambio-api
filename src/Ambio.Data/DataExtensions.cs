using Ambio.Data.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ambio.Data;

public static class DataExtensions
{
    public static void AddAmbioData(this IServiceCollection services, string migrationAssembly, string? connectionString)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TimestampInterceptor>();
        services.AddDbContext<AmbioDbContext>((provider, options) =>
            {
                options.UseNpgsql(connectionString, npgsl =>
                {
                    npgsl.MigrationsAssembly(migrationAssembly);
                });
                options.UseSnakeCaseNamingConvention();
                options.AddInterceptors(provider.GetRequiredService<TimestampInterceptor>());
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);
    }
}
