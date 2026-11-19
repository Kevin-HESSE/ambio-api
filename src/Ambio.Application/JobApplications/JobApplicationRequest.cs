
namespace Ambio.Application.JobApplications;

public record JobApplicationRequest(
    string Title,
    string Company,
    string Url,
    DateOnly AppliedOn
);
