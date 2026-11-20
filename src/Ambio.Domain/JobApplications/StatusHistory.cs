using Ambio.Domain.Abstractions;

namespace Ambio.Domain.JobApplications;

public class StatusHistory : ITimeStampable
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid JobApplicationId { get; init; }

    public required ApplicationStatus StatusFrom { get; init; }

    public required ApplicationStatus StatusTo { get; init; }

    public required DateOnly ChangedAt { get; init; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
