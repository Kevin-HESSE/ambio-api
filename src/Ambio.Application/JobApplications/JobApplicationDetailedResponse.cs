using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public record JobApplicationDetailedResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string? Url { get; init; }
    public DateOnly? AppliedOn { get; init; }
    public ApplicationStatus CurrentStatus { get; init; }
    public List<StatusHistoryResponse> History { get; init; } = [];
    public DateTime? ArchivedAt { get; init; }

    public JobApplicationDetailedResponse() { }

    public JobApplicationDetailedResponse(JobApplicationResponse jobApplication, IReadOnlyList<StatusHistory> history)
    {
        Id = jobApplication.Id;
        Title = jobApplication.Title;
        Company = jobApplication.Company;
        Kind = jobApplication.Kind;
        Url = jobApplication.Url;
        CurrentStatus = jobApplication.CurrentStatus;
        AppliedOn = jobApplication.AppliedOn;
        ArchivedAt = jobApplication.ArchivedAt;
        History = [.. history.Select(h => h.ToResponse())];
    }
}
