namespace TelegramPhotoBot.Domain.Enums;

/// <summary>
/// Defines the type of photo content
/// </summary>
public enum PhotoType
{
    /// <summary>
    /// Demo/preview photo - free for all users to view
    /// Each model can have one demo photo for showcasing
    /// </summary>
    Demo = 0,
    
    /// <summary>
    /// Premium photo - requires subscription or purchase
    /// Main monetized content
    /// </summary>
    Premium = 1
}

