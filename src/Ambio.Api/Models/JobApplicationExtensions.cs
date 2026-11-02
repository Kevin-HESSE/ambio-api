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

    public static JobApplication ToEntity(this JobApplicationRequest request) => new JobApplication
    {
        Title = request.Title,
        Company = request.Company,
        Url = request.Url,
        Status = request.Status,
        AppliedOn = request.AppliedAt
    };
}
