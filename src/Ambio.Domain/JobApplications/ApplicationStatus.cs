namespace Ambio.Domain.JobApplications;

/// <summary>Where an application stands: Sent → Interview → Offer / Rejected / Withdrawn.</summary>
public enum ApplicationStatus
{
    // Default status for a new application
    Created,
    // Sent, waiting for an answer
    Sent,
    // At least one interview planned or held
    Interview,
    // A job offer was received
    Offer,
    // A job offer was taken
    Taken,
    // Turned down by the employer
    Rejected,
    // Dropped by the candidate
    Withdrawn
}
