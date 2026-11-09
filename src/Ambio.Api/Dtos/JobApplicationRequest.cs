using Ambio.Domain.JobApplications;

namespace Ambio.Api.Dtos;

public record JobApplicationRequest(
    string Title,
    string Company,
    string Url,
    ApplicationStatus Status,
    DateOnly? AppliedAt
);
