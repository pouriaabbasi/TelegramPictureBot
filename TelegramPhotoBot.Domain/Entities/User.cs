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
    
    // Navigation properties
    private readonly List<UserRole> _userRoles = new();
    public virtual IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<Photo> _photos = new();
    public virtual IReadOnlyCollection<Photo> Photos => _photos.AsReadOnly();

    private readonly List<Subscription> _subscriptions = new();
    public virtual IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();

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

    // Methods for managing collections (EF Core backing fields)
    public void AddRole(UserRole userRole)
    {
        if (!_userRoles.Any(ur => ur.RoleId == userRole.RoleId))
        {
            _userRoles.Add(userRole);
        }
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
        }
    }
}
