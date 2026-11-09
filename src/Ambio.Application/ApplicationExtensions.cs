using Ambio.Application.JobApplications;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Ambio.Application;

public static class ApplicationExtensions
{
    public static void AddAmbioServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IAmbioApplication>();
        services.AddScoped<JobApplicationServices>();
    }
}
