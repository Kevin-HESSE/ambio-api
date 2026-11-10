using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public record JobApplicationRequest(
    string Title,
    string Company,
    string Url,
    string Kind,
    ApplicationStatus Status,
    DateOnly? AppliedAt
);
