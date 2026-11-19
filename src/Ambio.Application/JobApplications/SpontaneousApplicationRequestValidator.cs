using FluentValidation;

namespace Ambio.Application.JobApplications;

public class SpontaneousApplicationRequestValidator : AbstractValidator<SpontaneousApplicationRequest>
{
    public SpontaneousApplicationRequestValidator(TimeProvider timeProvider)
    {
        Include(new JobApplicationRequestValidator(timeProvider));
    }
}
