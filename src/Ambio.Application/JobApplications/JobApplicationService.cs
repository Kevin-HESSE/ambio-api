using Ambio.Application.Abstractions.OperationResult;
using Ambio.Data;
using Ambio.Domain.JobApplications;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Application.JobApplications;

public class JobApplicationService
{
    private readonly AmbioDbContext _ambioDbContext;
    private readonly IValidator<JobApplicationRequest> _validator;

    public JobApplicationService(AmbioDbContext ambioDbContext, IValidator<JobApplicationRequest> validator)
    {
        _ambioDbContext = ambioDbContext;
        _validator = validator;
    }

    public async Task<ResultFor<List<JobApplicationResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var jobApplications = await _ambioDbContext.JobApplications
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

    public async Task<ResultFor<JobApplicationResponse>> Create(JobApplicationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return ResultFor<JobApplicationResponse>.NotValid(validationResult.Errors);
        }

        var jobApplication = request.ToEntity();
        await _ambioDbContext.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return ResultFor<JobApplicationResponse>.Success(jobApplication.ToResponse());
    }

    public async Task<ResultFor<JobApplicationResponse>> Update(Guid id, JobApplicationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

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
