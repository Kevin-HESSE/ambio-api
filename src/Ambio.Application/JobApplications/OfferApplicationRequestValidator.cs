using Ambio.Application.Abstractions;

using FluentValidation;

namespace Ambio.Application.JobApplications;

public class OfferApplicationRequestValidator : AbstractValidator<OfferApplicationRequest>
{
    public OfferApplicationRequestValidator(TimeProvider timeProvider)
    {
        Include(new JobApplicationRequestValidator(timeProvider));

        RuleFor(ja => ja.Url)
            .MustBeAValidUrl()
            .OverridePropertyName("url");
    }
}
