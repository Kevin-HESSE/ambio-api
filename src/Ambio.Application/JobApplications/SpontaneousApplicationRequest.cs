namespace Ambio.Application.JobApplications;

public record SpontaneousApplicationRequest(
    string Title,
    string Company,
    DateOnly AppliedOn
) : JobApplicationRequest(Title, Company, AppliedOn);
