using Ambio.Api.Common;

namespace Ambio.Api.Models;

public class JobApplication : ITimeStampable
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Company { get; set; }
    public required string Url { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public DateOnly? AppliedOn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
