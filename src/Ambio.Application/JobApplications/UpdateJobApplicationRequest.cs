namespace Ambio.Application.JobApplications;

public record UpdateJobApplicationRequest(
    string Title,
    string Company,
    string? Url);
