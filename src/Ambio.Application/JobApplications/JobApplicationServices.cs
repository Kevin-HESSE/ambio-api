using Ambio.Data;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

namespace Ambio.Application.JobApplications;

public class JobApplicationServices
{
    private readonly AmbioDbContext _ambioDbContext;
    private readonly IValidator<JobApplicationRequest> _validator;

    public JobApplicationServices(AmbioDbContext ambioDbContext, IValidator<JobApplicationRequest> validator)
    {
        _ambioDbContext = ambioDbContext;
        _validator = validator;
    }

    public async Task<List<JobApplicationResponse>> GetAll()
    {
        return await _ambioDbContext.JobApplications
            .Select(ja => ja.ToResponse())
            .ToListAsync();
    }

    public async Task<JobApplicationResponse?> GetById(Guid id)
    {
        var jobApplication = await _ambioDbContext.JobApplications.FindAsync(id);
        return jobApplication?.ToResponse();
    }

    public async Task<JobApplicationResponse> Create(JobApplicationRequest request)
    {
        var jobApplication = request.ToEntity();
        await _ambioDbContext.JobApplications.AddAsync(jobApplication);
        await _ambioDbContext.SaveChangesAsync();

        return jobApplication.ToResponse();
    }

    public async Task<JobApplicationResponse?> Update(Guid id, JobApplicationRequest request)
    {
        var existingApplication = await _ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null)
        {
            return null;
        }

        existingApplication.Title = request.Title;
        existingApplication.Url = request.Url;
        existingApplication.Company = request.Company;
        existingApplication.Status = request.Status;
        existingApplication.AppliedOn = request.AppliedAt;

        await _ambioDbContext.SaveChangesAsync();

        return existingApplication.ToResponse();
    }

    public async Task<bool> Delete(Guid id)
    {
        var existingApplication = await _ambioDbContext.JobApplications.FindAsync(id);

        if (existingApplication is null) return false;

        _ambioDbContext.JobApplications.Remove(existingApplication);
        await _ambioDbContext.SaveChangesAsync();

        return true;
    }

    public async Task<ValidationResult> Validate(JobApplicationRequest jobApplicationRequest)
    {
        return await _validator.ValidateAsync(jobApplicationRequest);
    }

}
