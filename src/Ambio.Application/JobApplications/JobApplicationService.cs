using Ambio.Application.Abstractions.OperationResult;
using Ambio.Data;
using Ambio.Domain.JobApplications;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Application.JobApplications;

public class JobApplicationService
{
    private readonly AmbioDbContext _ambioDbContext;
    private readonly IValidator<JobApplicationRequest> _requestValidator;
    private readonly IValidator<IJobApplicationFilter> _filterValidator;

    public JobApplicationService(
        AmbioDbContext ambioDbContext,
        IValidator<JobApplicationRequest> requestValidator,
        IValidator<IJobApplicationFilter> filterValidator)
    {
        _ambioDbContext = ambioDbContext;
        _requestValidator = requestValidator;
        _filterValidator = filterValidator;
    }

    public async Task<ResultFor<List<JobApplicationResponse>>> GetAll(
        IJobApplicationFilter filter,
        CancellationToken cancellationToken)
    {
        var validationResult = await _filterValidator.ValidateAsync(filter, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<List<JobApplicationResponse>>.NotValid(validationResult.Errors);
        }

        var page = filter.Page ?? 1;
        var itemsPerPage = filter.ItemsPerPage ?? 10;

        IQueryable<JobApplication> query = _ambioDbContext.JobApplications;

        query = filter.Archived switch
        {
            ArchivedOption.Only => query.Where(ja => ja.ArchivedAt != null),
            ArchivedOption.Include => query.Where(ja => ja.ArchivedAt == null || ja.ArchivedAt != null),
            _ => query.Where(ja => ja.ArchivedAt == null)
        };

        if (filter.Company is not null)
        {
            query = query.Where(ja => ja.Company == filter.Company);
        }

        if (filter.Search is not null)
        {
            query = query.Where(ja => EF.Functions.ILike(ja.Title, $"%{filter.Search}%"));
        }

        query = filter.OrderByDate switch
        {
            OrderOption.Asc => query.OrderBy(ja => ja.CreatedAt),
            _ => query.OrderByDescending(ja => ja.CreatedAt)
        };

        query = filter.OrderByName switch
        {
            OrderOption.Desc => query.OrderByDescending(ja => ja.Title),
            OrderOption.Asc => query.OrderBy(ja => ja.Title),
            _ => query
        };

        var jobApplications = await query
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .Select(ja => ja.ToResponse())
            .ToListAsync(cancellationToken);

        return ResultFor<List<JobApplicationResponse>>.Success(jobApplications);
    }

    public async Task<ResultFor<JobApplicationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var jobApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);

        return jobApplication is not null
            ? ResultFor<JobApplicationResponse>.Success(jobApplication.ToResponse())
            : ResultFor<JobApplicationResponse>.NotFound();
    }

    public async Task<ResultFor<JobApplicationResponse>> Create(JobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _requestValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<JobApplicationResponse>.NotValid(validationResult.Errors);
        }

        var jobApplication = request.ToEntity();
        await _ambioDbContext.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<JobApplicationResponse>.Success(jobApplication.ToResponse());
    }

    public async Task<ResultFor<JobApplicationResponse>> Update(Guid id, JobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _requestValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<JobApplicationResponse>.NotValid(validationResult.Errors);
        }

        var existingApplication = await _ambioDbContext.JobApplications
            .OfType<OfferApplication>()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (existingApplication is null)
        {
            return ResultFor<JobApplicationResponse>.NotFound();
        }

        existingApplication.Title = request.Title;
        existingApplication.Url = request.Url;
        existingApplication.Company = request.Company;
        existingApplication.Status = request.Status;
        existingApplication.AppliedOn = request.AppliedAt;

        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<JobApplicationResponse>.Success(existingApplication.ToResponse());
    }

    public async Task<ResultFor<bool>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existingApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);

        if (existingApplication is null) return ResultFor<bool>.NotFound();

        _ambioDbContext.JobApplications.Remove(existingApplication);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<bool>.Success(true);
    }
}
