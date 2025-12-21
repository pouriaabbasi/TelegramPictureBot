using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public class Photo : AggregateRoot
{
    public FileInfo FileInfo { get; private set; }
    public string? Caption { get; private set; }
    public TelegramStars Price { get; private set; }
    public bool IsForSale { get; private set; } = true;
    public Guid SellerId { get; private set; }
    
    // Navigation properties
    public virtual User Seller { get; private set; } = null!;

    // EF Core constructor
    protected Photo() { }

    public Photo(
        FileInfo fileInfo,
        Guid sellerId,
        TelegramStars price,
        string? caption = null)
    {
        FileInfo = fileInfo;
        SellerId = sellerId;
        Price = price;
        Caption = caption;
    }

    public void UpdatePrice(TelegramStars newPrice)
    {
        Price = newPrice;
        MarkAsUpdated();
    }

    public void UpdateCaption(string? caption)
    {
        Caption = caption;
        MarkAsUpdated();
    }

    public void MarkForSale()
    {
        IsForSale = true;
        MarkAsUpdated();
    }

    public void MarkAsNotForSale()
    {
        IsForSale = false;
        MarkAsUpdated();
    }

    public bool CanBePurchased()
    {
        return IsForSale && !IsDeleted;
    }
}
