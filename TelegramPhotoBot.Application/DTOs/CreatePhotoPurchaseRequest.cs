namespace TelegramPhotoBot.Application.DTOs;

/// <summary>
/// Request for creating a photo purchase
/// </summary>
public class CreatePhotoPurchaseRequest
{
    public Guid UserId { get; init; }
    public Guid PhotoId { get; init; }
}

