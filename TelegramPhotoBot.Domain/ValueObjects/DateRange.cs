namespace TelegramPhotoBot.Domain.ValueObjects;

public record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date", nameof(endDate));

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool IsActive(DateTime? date = null)
    {
        var checkDate = date ?? DateTime.UtcNow;
        return checkDate >= StartDate && checkDate <= EndDate;
    }

    public bool IsExpired(DateTime? date = null)
    {
        var checkDate = date ?? DateTime.UtcNow;
        return checkDate > EndDate;
    }

    public int DaysRemaining(DateTime? date = null)
    {
        var checkDate = date ?? DateTime.UtcNow;
        if (IsExpired(checkDate))
            return 0;

        return (EndDate - checkDate).Days;
    }

    public int TotalDays => (EndDate - StartDate).Days;
}

