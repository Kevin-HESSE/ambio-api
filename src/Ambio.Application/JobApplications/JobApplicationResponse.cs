using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public record JobApplicationResponse(
    Guid Id,
    string Title,
    string Company,
    string Kind,
    string? Url,
    ApplicationStatus CurrentStatus,
    DateOnly? AppliedOn,
    DateTime? ArchivedAt
    );
