namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Defines user roles in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular user who can browse and purchase content
    /// </summary>
    User = 0,
    
    /// <summary>
    /// Content creator who can upload and sell content
    /// Must be approved by admin before becoming active
    /// </summary>
    Model = 1,
    
    /// <summary>
    /// Platform administrator with full permissions
    /// Can approve models, manage users, moderate content
    /// </summary>
    Admin = 2
}

