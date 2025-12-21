using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public abstract class Purchase : BaseEntity
{
    public Guid UserId { get; protected set; }
    public TelegramStars Amount { get; protected set; }
    public DateTime PurchaseDate { get; protected set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; protected set; } = null!;

    // EF Core constructor
    protected Purchase() { }

    protected Purchase(Guid userId, TelegramStars amount)
    {
        UserId = userId;
        Amount = amount;
        PurchaseDate = DateTime.UtcNow;
    }

    public abstract PurchaseType GetPurchaseType();
}
