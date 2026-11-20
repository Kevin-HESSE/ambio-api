using Ambio.Domain.JobApplications;

using FluentValidation;

namespace Ambio.Application.JobApplications;

public class StatusRequestValidator : AbstractValidator<StatusRequest>
{
    public StatusRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .IsEnumName(typeof(ApplicationStatus), caseSensitive: false)
            .OverridePropertyName("status");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime))
            .When(x => x.Date is not null)
            .OverridePropertyName("date");
    }
}
