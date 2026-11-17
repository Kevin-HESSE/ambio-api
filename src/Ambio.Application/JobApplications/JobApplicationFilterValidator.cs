using Ambio.Application.Filters;
using Ambio.Domain.JobApplications;

using FluentValidation;

namespace Ambio.Application.JobApplications;

public class JobApplicationFilterValidator : AbstractValidator<IJobApplicationFilter>
{
    public JobApplicationFilterValidator()
    {
        RuleFor(f => f.Archived)
            .IsEnumName(typeof(ArchivedOption), caseSensitive: false)
            .When(f => !string.IsNullOrWhiteSpace(f.Archived))
            .OverridePropertyName("archived")
            .WithMessage("Invalid archived option, use none, only, or included");

        RuleForEach(f => f.Status)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .IsEnumName(typeof(ApplicationStatus), caseSensitive: false)
            .OverridePropertyName("status")
            .WithMessage("Invalid status option, use sent, interview, offer, taken, rejected or withdrawn. " +
                         "Repeat the parameter for several values");

        RuleFor(f => f.OrderByDate)
            .IsEnumName(typeof(OrderOption), caseSensitive: false)
            .When(f => !string.IsNullOrWhiteSpace(f.OrderByDate))
            .OverridePropertyName("order[date]")
            .WithMessage("Invalid order by date option, use asc or desc");

        RuleFor(f => f.OrderByTitle)
            .IsEnumName(typeof(OrderOption), caseSensitive: false)
            .When(f => !string.IsNullOrWhiteSpace(f.OrderByTitle))
            .OverridePropertyName("order[title]")
            .WithMessage("Invalid order by title option, use asc or desc");

        Include(new PaginationFilterValidator());
    }
}
