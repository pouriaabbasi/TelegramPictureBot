namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Subscription data transfer object
/// </summary>
public class SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; }
    public int DaysRemaining { get; init; }
}

