namespace TelegramPhotoBot.Application.DTOs;

public class DetailedContactStatus
{
    public bool IsContact { get; set; }
    public bool IsMutualContact { get; set; }
    public bool IsAutoAddSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

