using FluentValidation;

namespace Ambio.Application.JobApplications;

public class JobApplicationRequestValidator : AbstractValidator<JobApplicationRequest>
{
    public JobApplicationRequestValidator()
    {
        RuleFor(ja => ja.Title).NotEmpty();
        RuleFor(ja => ja.Company).NotEmpty();
        RuleFor(ja => ja.Url).NotEmpty();
        RuleFor(ja => ja.Status).IsInEnum();
    }
}
