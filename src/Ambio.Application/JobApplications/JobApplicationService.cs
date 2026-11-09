using Ambio.Data;

using FluentValidation;
using FluentValidation.Results;

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

    public async Task<List<JobApplicationResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await _ambioDbContext.JobApplications
            .Select(ja => ja.ToResponse())
            .ToListAsync(cancellationToken);
    }

    public async Task<JobApplicationResponse?> GetById(Guid id, CancellationToken cancellationToken)
    {
        var jobApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);
        return jobApplication?.ToResponse();
    }

    public async Task<JobApplicationResponse> Create(JobApplicationRequest request, CancellationToken cancellationToken)
    {
        var jobApplication = request.ToEntity();
        await _ambioDbContext.JobApplications.AddAsync(jobApplication, cancellationToken);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return jobApplication.ToResponse();
    }

    public async Task<JobApplicationResponse?> Update(Guid id, JobApplicationRequest request, CancellationToken cancellationToken)
    {
        var existingApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);

        if (existingApplication is null)
        {
            return null;
        }

        existingApplication.Title = request.Title;
        existingApplication.Url = request.Url;
        existingApplication.Company = request.Company;
        existingApplication.Status = request.Status;
        existingApplication.AppliedOn = request.AppliedAt;

        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return existingApplication.ToResponse();
    }

    public async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existingApplication = await _ambioDbContext.JobApplications
            .FindAsync([id], cancellationToken: cancellationToken);

        if (existingApplication is null) return false;

        _ambioDbContext.JobApplications.Remove(existingApplication);
        await _ambioDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ValidationResult> Validate(JobApplicationRequest jobApplicationRequest)
    {
        return await _validator.ValidateAsync(jobApplicationRequest);
    }
}
