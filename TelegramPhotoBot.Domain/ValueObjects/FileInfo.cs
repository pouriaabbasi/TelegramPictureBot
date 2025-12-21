namespace TelegramPhotoBot.Domain.ValueObjects;

public record FileInfo
{
    public string FileId { get; init; }
    public string? FileUniqueId { get; init; }
    public string? FilePath { get; init; }
    public string? MimeType { get; init; }
    public long? FileSize { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }

    public FileInfo(
        string fileId,
        string? fileUniqueId = null,
        string? filePath = null,
        string? mimeType = null,
        long? fileSize = null,
        int? width = null,
        int? height = null)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId cannot be null or empty", nameof(fileId));

        FileId = fileId;
        FileUniqueId = fileUniqueId;
        FilePath = filePath;
        MimeType = mimeType;
        FileSize = fileSize;
        Width = width;
        Height = height;
    }

    public bool IsImage => MimeType?.StartsWith("image/") ?? false;
    public bool HasDimensions => Width.HasValue && Height.HasValue;
}

