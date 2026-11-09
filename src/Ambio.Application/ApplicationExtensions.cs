using Ambio.Application.JobApplications;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Ambio.Application;

public static class ApplicationExtensions
{
    public static void AddAmbioApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);
        services.AddScoped<JobApplicationService>();
    }
}
