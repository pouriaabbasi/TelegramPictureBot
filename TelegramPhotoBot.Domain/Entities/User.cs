using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Domain.Entities;

public class User : AggregateRoot
{
    public TelegramUserId TelegramUserId { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? LanguageCode { get; private set; }
    public bool IsBot { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    // Role management (marketplace feature)
    public UserRole Role { get; private set; }
    
    // Model relationship (if user is a model)
    public Guid? ModelId { get; private set; }
    public Model? Model { get; private set; }
    
    // Navigation properties

    private readonly List<Photo> _photos = new();
    public virtual IReadOnlyCollection<Photo> Photos => _photos.AsReadOnly();
    
    private readonly List<ModelSubscription> _modelSubscriptions = new();
    public virtual IReadOnlyCollection<ModelSubscription> ModelSubscriptions => _modelSubscriptions.AsReadOnly();

    private readonly List<Purchase> _purchases = new();
    public virtual IReadOnlyCollection<Purchase> Purchases => _purchases.AsReadOnly();

    // EF Core constructor
    protected User() { }

    public User(
        TelegramUserId telegramUserId,
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        string? languageCode = null,
        bool isBot = false)
    {
        TelegramUserId = telegramUserId;
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        LanguageCode = languageCode;
        IsBot = isBot;
        Role = UserRole.User; // Default role
    }

    public void UpdateProfile(string? firstName, string? lastName, string? username)
    {
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Role management methods
    
    public void PromoteToModel(Guid modelId)
    {
        if (Role == UserRole.Admin)
            throw new InvalidOperationException("Admin cannot be demoted to model");
            
        Role = UserRole.Model;
        ModelId = modelId;
        MarkAsUpdated();
    }

    public void PromoteToAdmin()
    {
        Role = UserRole.Admin;
        ModelId = null; // Admins cannot be models
        MarkAsUpdated();
    }

    public void DemoteToUser()
    {
        if (Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot demote admin to user. Revoke admin status first.");
            
        Role = UserRole.User;
        ModelId = null;
        MarkAsUpdated();
    }

    public bool IsModel() => Role == UserRole.Model && ModelId.HasValue;
    
    public bool IsAdmin() => Role == UserRole.Admin;
    
    public bool CanAccessModelFeatures() => Role == UserRole.Model || Role == UserRole.Admin;
    
    public bool CanAccessAdminFeatures() => Role == UserRole.Admin;
}
