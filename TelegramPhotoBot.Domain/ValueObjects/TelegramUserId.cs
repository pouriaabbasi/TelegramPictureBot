namespace TelegramPhotoBot.Domain.ValueObjects;

public record TelegramUserId
{
    public long Value { get; init; }

    public TelegramUserId(long value)
    {
        if (value <= 0)
            throw new ArgumentException("Telegram User ID must be greater than zero", nameof(value));

        Value = value;
    }

    public static implicit operator long(TelegramUserId userId) => userId.Value;
    public static implicit operator TelegramUserId(long value) => new(value);
}

