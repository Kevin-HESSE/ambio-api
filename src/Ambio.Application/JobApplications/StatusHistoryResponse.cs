using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public record StatusHistoryResponse(
    ApplicationStatus Status,
    DateOnly Date
);
