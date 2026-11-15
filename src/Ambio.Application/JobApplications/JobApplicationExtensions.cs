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
        Status: application.Status,
        AppliedAt: application.AppliedOn,
        ArchivedAt: application.ArchivedAt
    );

    private static JobApplicationResponse ToResponse(this SpontaneousApplication application) => new(
        Id: application.Id,
        Title: application.Title,
        Company: application.Company,
        Kind: nameof(ApplicationKind.Spontaneous),
        Url: null,
        Status: application.Status,
        AppliedAt: application.AppliedOn,
        ArchivedAt: application.ArchivedAt
    );

    extension(JobApplicationRequest request)
    {
        public JobApplication ToEntity()
        {
            return request.Kind switch
            {
                "offer" => request.ToOfferApplication(),
                "spontaneous" => request.ToSpontaneousApplication(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private SpontaneousApplication ToSpontaneousApplication() => new()
        {
            Title = request.Title,
            Company = request.Company,
            Status = request.Status,
            AppliedOn = request.AppliedAt
        };

        private OfferApplication ToOfferApplication() => new()
        {
            Title = request.Title,
            Url = request.Url,
            Company = request.Company,
            Status = request.Status,
            AppliedOn = request.AppliedAt
        };
    }
}
