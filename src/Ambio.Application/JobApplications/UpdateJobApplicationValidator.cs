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
            .MaximumLength(128)
            .OverridePropertyName("title");

        RuleFor(x => x.Company)
            .NotEmpty()
            .MaximumLength(128)
            .OverridePropertyName("company");

        RuleSet(OfferApplicationRules, () => RuleFor(x => x.Url)
            .MaximumLength(2048)
            .MustBeAValidUrl()
            .OverridePropertyName("url"));

        RuleSet(SpontaneousApplicationRules, () => RuleFor(x => x.Url)
            .Null()
            .OverridePropertyName("url"));
    }
}
