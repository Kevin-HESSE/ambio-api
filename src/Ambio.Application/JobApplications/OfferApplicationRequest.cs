
namespace Ambio.Application.JobApplications;

public record OfferApplicationRequest(
    string Title,
    string Company,
    string Url,
    DateOnly AppliedOn
) : JobApplicationRequest(Title, Company, AppliedOn);
