using FluentValidation;

namespace Ambio.Application.JobApplications;

public class JobApplicationRequestValidator : AbstractValidator<JobApplicationRequest>
{
    public JobApplicationRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(ja => ja.Title)
            .NotEmpty()
            .OverridePropertyName("title");

        RuleFor(ja => ja.Company)
            .NotEmpty()
            .OverridePropertyName("company");

        RuleFor(ja => ja.AppliedOn)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime))
            .OverridePropertyName("appliedOn");
    }
}
