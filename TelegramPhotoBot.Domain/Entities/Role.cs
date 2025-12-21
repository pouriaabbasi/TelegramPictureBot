namespace TelegramPhotoBot.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    
    // Navigation properties
    private readonly List<UserRole> _userRoles = new();
    public virtual IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // EF Core constructor
    protected Role() { }

    public Role(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
        Description = description;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        MarkAsUpdated();
    }
}
