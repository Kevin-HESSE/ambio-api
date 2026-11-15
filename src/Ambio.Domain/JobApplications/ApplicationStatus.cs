namespace Ambio.Domain.JobApplications;

/// <summary>Where an application stands: Applied → Interview → Offer / Rejected / Withdrawn.</summary>
public enum ApplicationStatus
{
    // Sent, waiting for an answer
    Applied,
    // At least one interview planned or held
    Interview,
    // A job offer was received
    Offer,
    // Turned down by the employer
    Rejected,
    // Dropped by the candidate
    Withdrawn
}
