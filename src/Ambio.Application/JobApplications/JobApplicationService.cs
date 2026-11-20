using Ambio.Application.Abstractions;
using Ambio.Application.Abstractions.OperationResult;
using Ambio.Application.Filters;
using Ambio.Data;
using Ambio.Domain.JobApplications;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Application.JobApplications;

public class JobApplicationService
{
    private readonly AmbioDbContext _ambioDbContext;
    private readonly IValidator<OfferApplicationRequest> _offerRequestValidator;
    private readonly IValidator<SpontaneousApplicationRequest> _spontaneousRequestValidator;
    private readonly IValidator<UpdateJobApplicationRequest> _updateRequestValidator;
    private readonly IValidator<IJobApplicationFilter> _filterValidator;
    private readonly TimeProvider _timeProvider;

    public JobApplicationService(
        AmbioDbContext ambioDbContext,
        IValidator<OfferApplicationRequest> offerRequestValidator,
        IValidator<SpontaneousApplicationRequest> spontaneousRequestValidator,
        IValidator<UpdateJobApplicationRequest> updateRequestValidator,
        IValidator<IJobApplicationFilter> filterValidator,
        TimeProvider timeProvider)
    {
        _ambioDbContext = ambioDbContext;
        _offerRequestValidator = offerRequestValidator;
        _spontaneousRequestValidator = spontaneousRequestValidator;
        _updateRequestValidator = updateRequestValidator;
        _filterValidator = filterValidator;
        _timeProvider = timeProvider;
    }

    public async Task<ResultFor<CollectionResponse<JobApplicationResponse>>> GetAll(
        IJobApplicationFilter filter,
        CancellationToken cancellationToken)
    {
        var validationResult = await _filterValidator.ValidateAsync(filter, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<CollectionResponse<JobApplicationResponse>>.NotValid(validationResult.Errors);
        }

        var page = filter.Page ?? 1;
        var itemsPerPage = filter.ItemsPerPage ?? 10;

        var query = _ambioDbContext.JobApplications
            .AsNoTracking();

        query = EnumFilter.Parse<ArchivedOption>(filter.Archived) switch
        {
            ArchivedOption.Only => query.Where(ja => ja.ArchivedAt != null),
            ArchivedOption.Included => query,
            // Also covers a missing parameter: non-archived applications by default
            _ => query.Where(ja => ja.ArchivedAt == null)
        };

        if (!string.IsNullOrWhiteSpace(filter.Company))
        {
            var company = EscapeLikePattern(filter.Company);
            query = query.Where(ja => EF.Functions.ILike(ja.Company, company, LikeEscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{EscapeLikePattern(filter.Search)}%";
            query = query.Where(ja => EF.Functions.ILike(ja.Title, search, LikeEscapeCharacter));
        }

        var statuses = (filter.Status ?? [])
            .Select(EnumFilter.Parse<ApplicationStatus>)
            .OfType<ApplicationStatus>()
            .ToList();

        if (statuses.Count > 0)
        {
            query = query.Where(ja => statuses.Contains(ja.Status));
        }

        IOrderedQueryable<JobApplication> orderedQuery = EnumFilter.Parse<OrderOption>(filter.OrderByDate) switch
        {
            OrderOption.Asc => query.OrderBy(ja => ja.CreatedAt),
            _ => query.OrderByDescending(ja => ja.CreatedAt)
        };

        orderedQuery = EnumFilter.Parse<OrderOption>(filter.OrderByTitle) switch
        {
            OrderOption.Desc => orderedQuery.ThenByDescending(ja => ja.Title),
            OrderOption.Asc => orderedQuery.ThenBy(ja => ja.Title),
            _ => orderedQuery
        };

        var totalJobApplication = await query.CountAsync(cancellationToken);

        var lastPage = Math.Max(1, (int)Math.Ceiling(totalJobApplication / (double)itemsPerPage));
        var effectivePage = Math.Clamp(page, 1, lastPage);

        var jobApplications = await orderedQuery
            .ThenBy(ja => ja.Id)
            .Skip((effectivePage - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .Select(ja => ja.ToResponse())
            .ToListAsync(cancellationToken);

        var result = new CollectionResponse<JobApplicationResponse>(
            Items: jobApplications,
            Total: totalJobApplication,
            ItemsPerPage: itemsPerPage,
            CurrentPage: effectivePage,
            MaxPage: lastPage);

        return ResultFor<CollectionResponse<JobApplicationResponse>>.Success(result);
    }

    public async Task<ResultFor<JobApplicationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var jobApplication = await _ambioDbContext.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(ja => ja.Id == id, cancellationToken: cancellationToken);

        return jobApplication is not null
            ? ResultFor<JobApplicationResponse>.Success(jobApplication.ToResponse())
            : ResultFor<JobApplicationResponse>.NotFound();
    }

    public async Task<ResultFor<JobApplicationResponse>> CreateOffer(OfferApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _offerRequestValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<JobApplicationResponse>.NotValid(validationResult.Errors);
        }

        var jobApplication = request.ToEntity();
        await _ambioDbContext.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<JobApplicationResponse>.Success(jobApplication.ToResponse());
    }

    public async Task<ResultFor<JobApplicationResponse>> CreateSpontaneous(
        SpontaneousApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _spontaneousRequestValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<JobApplicationResponse>.NotValid(validationResult.Errors);
        }

        var jobApplication = request.ToEntity();
        await _ambioDbContext.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<JobApplicationResponse>.Success(jobApplication.ToResponse());
    }

    public async Task<ResultFor<JobApplicationResponse>> Update(Guid id, UpdateJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var existingApplication = await _ambioDbContext.JobApplications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (existingApplication is null)
        {
            return ResultFor<JobApplicationResponse>.NotFound();
        }

        if (existingApplication.IsArchived)
        {
            return ResultFor<JobApplicationResponse>.AlreadyArchived();
        }

        var appliedRules = existingApplication switch
        {
            OfferApplication => UpdateJobApplicationValidator.OfferApplicationRules,
            SpontaneousApplication => UpdateJobApplicationValidator.SpontaneousApplicationRules,
            _ => throw new InvalidOperationException("Unknown job application type")
        };

        var validationResult = await _updateRequestValidator.ValidateAsync(
            request,
            options => options.IncludeRulesNotInRuleSet().IncludeRuleSets(appliedRules),
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<JobApplicationResponse>.NotValid(validationResult.Errors);
        }

        existingApplication.Title = request.Title;
        existingApplication.Company = request.Company;

        if (existingApplication is OfferApplication application)
        {
            application.Url = request.Url!;
        }

        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<JobApplicationResponse>.Success(existingApplication.ToResponse());
    }

    public async Task<ResultFor<bool>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var existingApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);

        if (existingApplication is null) return ResultFor<bool>.NotFound();

        if (existingApplication.IsArchived) return ResultFor<bool>.AlreadyArchived();

        existingApplication.Archive(_timeProvider.GetUtcNow().UtcDateTime);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<bool>.Success(true);
    }

    public async Task<ResultFor<bool>> Reopen(Guid id, CancellationToken cancellationToken)
    {
        var existingApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);

        if (existingApplication is null) return ResultFor<bool>.NotFound();

        if (!existingApplication.IsArchived) return ResultFor<bool>.NotArchived();

        existingApplication.Reopen();
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<bool>.Success(true);
    }

    // Npgsql emits ESCAPE '' when no escape character is given, so it has to be explicit
    private const string LikeEscapeCharacter = @"\";

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
