using Ambio.Domain.Abstractions;

namespace Ambio.Domain.JobApplications;

public abstract class JobApplication : ITimeStampable
{
    private readonly List<StatusHistory> _statusHistory = [];

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Title { get; set; }
    public required string Company { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Sent;
    public DateOnly AppliedOn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public bool IsArchived => ArchivedAt is not null;
    public void Archive(DateTime archivedAt) => ArchivedAt = archivedAt;
    public void Reopen() => ArchivedAt = null;

    public IReadOnlyList<StatusHistory> StatusHistory => _statusHistory;

    public bool CanChangeStatus(ApplicationStatus to) => !IsArchived && Status != to;
    public bool ChangeStatus(ApplicationStatus to, DateOnly changedAt)
    {
        if (!StatusHistoryWorkflow.Allows(Status, to))
            return false;

        var lastStatusHistory = _statusHistory.LastOrDefault();

        if (lastStatusHistory is not null && lastStatusHistory.ChangedAt > changedAt)
            return false;

        _statusHistory.Add(new StatusHistory
        {
            JobApplicationId = Id,
            StatusFrom = Status,
            StatusTo = to,
            ChangedAt = changedAt,
        });

        Status = to;
        return true;
    }

    public void MarkAsSent()
    {
        var isAlreadySent = _statusHistory.Any(x => x.StatusTo == ApplicationStatus.Sent);
        if (isAlreadySent)
            return;

        _statusHistory.Add(new StatusHistory
        {
            JobApplicationId = Id,
            StatusFrom = ApplicationStatus.Created,
            StatusTo = ApplicationStatus.Sent,
            ChangedAt = AppliedOn,
        });
    }
}
