using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public class PurchasePhoto : Purchase
{
    public Guid PhotoId { get; private set; }
    
    // Navigation properties
    public virtual Photo Photo { get; private set; } = null!;

    // EF Core constructor
    protected PurchasePhoto() { }

    public PurchasePhoto(Guid userId, Guid photoId, TelegramStars amount)
        : base(userId, amount)
    {
        PhotoId = photoId;
    }

    public override PurchaseType GetPurchaseType() => PurchaseType.SinglePhoto;
}

