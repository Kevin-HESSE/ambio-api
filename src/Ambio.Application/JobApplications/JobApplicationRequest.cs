namespace Ambio.Application.JobApplications;

public abstract record JobApplicationRequest(
    string Title,
    string Company,
    DateOnly AppliedOn);
