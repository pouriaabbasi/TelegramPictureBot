using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;
using FileInfo = TelegramPhotoBot.Domain.ValueObjects.FileInfo;

namespace TelegramPhotoBot.Domain.Entities;

public class Photo : AggregateRoot
{
    public FileInfo FileInfo { get; private set; }
    public string? Caption { get; private set; }
    public TelegramStars Price { get; private set; }
    public bool IsForSale { get; private set; } = true;
    public Guid SellerId { get; private set; }
    
    // Model scoping (marketplace feature)
    public Guid ModelId { get; private set; }
    public PhotoType Type { get; private set; }
    
    // Analytics
    public int ViewCount { get; private set; } = 0;
    
    // MTProto cached data (برای جلوگیری از upload مجدد)
    public long? MtProtoPhotoId { get; private set; }
    public long? MtProtoAccessHash { get; private set; }
    public byte[]? MtProtoFileReference { get; private set; }
    
    // Navigation properties
    public virtual User Seller { get; private set; } = null!;
    public virtual Model Model { get; private set; } = null!;

    // EF Core constructor
    protected Photo() { }

    public Photo(
        FileInfo fileInfo,
        Guid sellerId,
        Guid modelId,
        TelegramStars price,
        PhotoType type = PhotoType.Premium,
        string? caption = null)
    {
        FileInfo = fileInfo;
        SellerId = sellerId;
        ModelId = modelId;
        Price = price;
        Type = type;
        Caption = caption;
        
        // Demo photos are not for sale (they're free previews)
        if (type == PhotoType.Demo)
        {
            IsForSale = false;
        }
    }

    public void UpdatePrice(TelegramStars newPrice)
    {
        if (Type == PhotoType.Demo)
            throw new InvalidOperationException("Demo photos cannot have a price");
            
        Price = newPrice;
        MarkAsUpdated();
    }

    public void UpdateCaption(string? caption)
    {
        Caption = caption;
        MarkAsUpdated();
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        MarkAsUpdated();
    }

    public void MarkForSale()
    {
        if (Type == PhotoType.Demo)
            throw new InvalidOperationException("Demo photos cannot be marked for sale");
            
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
        return IsForSale && !IsDeleted && Type == PhotoType.Premium;
    }
    
    public bool IsDemoPhoto()
    {
        return Type == PhotoType.Demo;
    }
    
    public bool RequiresPayment()
    {
        return Type == PhotoType.Premium;
    }
    
    public void SetMtProtoPhotoInfo(long photoId, long accessHash, byte[] fileReference)
    {
        MtProtoPhotoId = photoId;
        MtProtoAccessHash = accessHash;
        MtProtoFileReference = fileReference;
        MarkAsUpdated();
    }
    
    public bool HasMtProtoPhotoInfo()
    {
        return MtProtoPhotoId.HasValue && MtProtoAccessHash.HasValue && MtProtoFileReference != null;
    }
}
