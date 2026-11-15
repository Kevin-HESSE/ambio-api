using FluentValidation;

namespace Ambio.Application.Filters;

public class PaginationFilterValidator : AbstractValidator<IPaginationFilter>
{
    public PaginationFilterValidator()
    {
        RuleFor(filter => filter.Page).GreaterThanOrEqualTo(1);
        RuleFor(filter => filter.ItemsPerPage).InclusiveBetween(1, 100);
    }
}
