namespace TelegramPhotoBot.Domain.ValueObjects;

/// <summary>
/// Value Object برای Telegram Stars - واحد پولی تلگرام
/// </summary>
public record TelegramStars
{
    public long Amount { get; init; }

    public TelegramStars(long amount)
    {
        if (amount < 0)
            throw new ArgumentException("Stars amount cannot be negative", nameof(amount));

        Amount = amount;
    }

    public TelegramStars Add(TelegramStars other)
    {
        return new TelegramStars(Amount + other.Amount);
    }

    public TelegramStars Subtract(TelegramStars other)
    {
        if (Amount < other.Amount)
            throw new InvalidOperationException("Insufficient stars");

        return new TelegramStars(Amount - other.Amount);
    }

    public TelegramStars Multiply(long factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative", nameof(factor));

        return new TelegramStars(Amount * factor);
    }

    public bool IsGreaterThan(TelegramStars other)
    {
        return Amount > other.Amount;
    }

    public bool IsGreaterThanOrEqual(TelegramStars other)
    {
        return Amount >= other.Amount;
    }

    public bool IsLessThan(TelegramStars other)
    {
        return Amount < other.Amount;
    }

    public bool IsLessThanOrEqual(TelegramStars other)
    {
        return Amount <= other.Amount;
    }

    public static TelegramStars Zero => new(0);
}

