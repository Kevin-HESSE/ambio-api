using Ambio.Application.Abstractions;

using FluentValidation;

namespace Ambio.Application.JobApplications;

public class UpdateJobApplicationValidator : AbstractValidator<UpdateJobApplicationRequest>
{
    public const string OfferApplicationRules = "Offer";
    public const string SpontaneousApplicationRules = "Spontaneous";

    public UpdateJobApplicationValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .OverridePropertyName("title");

        RuleFor(x => x.Company)
            .NotEmpty()
            .OverridePropertyName("company");

        RuleSet(OfferApplicationRules, () => RuleFor(x => x.Url)
            .MustBeAValidUrl()
            .OverridePropertyName("url"));

        RuleSet(SpontaneousApplicationRules, () => RuleFor(x => x.Url)
            .Null()
            .OverridePropertyName("url"));
    }
}
