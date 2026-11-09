using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public record JobApplicationRequest(
    string Title,
    string Company,
    string Url,
    ApplicationStatus Status,
    DateOnly? AppliedAt
);
