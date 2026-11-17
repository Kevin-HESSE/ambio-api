using Ambio.Domain.Abstractions;

namespace Ambio.Domain.JobApplications;

public abstract class JobApplication : ITimeStampable
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Title { get; set; }
    public required string Company { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Sent;
    public DateOnly? AppliedOn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
