using Ambio.Api.Dtos;

namespace Ambio.Api.Models;

public static class JobApplicationExtensions
{
    public static JobApplicationResponse ToResponse(this JobApplication application) => new (
        Id: application.Id,
        Title: application.Title,
        Company: application.Company,
        Url: application.Url,
        Status: application.Status.ToLabel(),
        AppliedAt: application.AppliedOn
    );
}
