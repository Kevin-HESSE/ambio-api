using Ambio.Domain.JobApplications;

namespace Ambio.Application.JobApplications;

public static class JobApplicationExtensions
{
    public static JobApplicationResponse ToResponse(this JobApplication application)
    {
        return application switch
        {
            OfferApplication offerApplication => offerApplication.ToResponse(),
            SpontaneousApplication spontaneousApplication => spontaneousApplication.ToResponse(),
            _ => throw new ArgumentOutOfRangeException(nameof(application))
        };
    }

    private static JobApplicationResponse ToResponse(this OfferApplication application) => new(
        Id: application.Id,
        Title: application.Title,
        Company: application.Company,
        Kind: nameof(ApplicationKind.Offer),
        Url: application.Url,
        CurrentStatus: application.Status,
        AppliedOn: application.AppliedOn,
        ArchivedAt: application.ArchivedAt
    );

    private static JobApplicationResponse ToResponse(this SpontaneousApplication application) => new(
        Id: application.Id,
        Title: application.Title,
        Company: application.Company,
        Kind: nameof(ApplicationKind.Spontaneous),
        Url: null,
        CurrentStatus: application.Status,
        AppliedOn: application.AppliedOn,
        ArchivedAt: application.ArchivedAt
    );

    public static OfferApplication ToEntity(this OfferApplicationRequest request) => new()
    {
        Title = request.Title,
        Url = request.Url,
        Company = request.Company,
        AppliedOn = request.AppliedOn
    };

    public static SpontaneousApplication ToEntity(this SpontaneousApplicationRequest request) => new()
    {
        Title = request.Title,
        Company = request.Company,
        AppliedOn = request.AppliedOn,
    };

    public static JobApplicationDetailedResponse ToDetailedResponse(this JobApplication application) =>
        new(application.ToResponse(), application.StatusHistory);

    public static StatusHistoryResponse ToResponse(this StatusHistory history) => new(
        Status: history.StatusTo,
        Date: history.ChangedAt);
}
