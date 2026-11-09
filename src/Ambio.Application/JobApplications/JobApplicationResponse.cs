namespace Ambio.Application.JobApplications;

public record JobApplicationResponse(
    Guid Id,
    string Title,
    string Company,
    string Url,
    string Status,
    DateOnly? AppliedAt
    );
