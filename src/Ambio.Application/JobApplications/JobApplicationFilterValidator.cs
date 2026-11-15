using Ambio.Application.Filters;

using FluentValidation;

namespace Ambio.Application.JobApplications;

public class JobApplicationFilterValidator : AbstractValidator<IJobApplicationFilter>
{
    public JobApplicationFilterValidator()
    {
        RuleFor(f => f.Archived)
            .IsInEnum()
            .When(x => x.Archived is not null)
            .WithMessage("Invalid archived option, use None, Only, or Included");

        RuleFor(f => f.Status)
            .IsInEnum()
            .When(x => x.Status is not null);

        RuleFor(f => f.OrderByDate)
            .IsInEnum()
            .When(x => x.OrderByDate is not null)
            .WithMessage("Invalid order by date option, use Asc or Desc");

        RuleFor(f => f.OrderByName)
            .IsInEnum()
            .When(x => x.OrderByName is not null)
            .WithMessage("Invalid order by name option, use Asc or Desc");

        Include(new PaginationFilterValidator());
    }
}
