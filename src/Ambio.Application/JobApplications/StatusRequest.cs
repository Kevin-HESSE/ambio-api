namespace Ambio.Application.JobApplications;

public record StatusRequest(
    string Status,
    DateOnly? Date);
