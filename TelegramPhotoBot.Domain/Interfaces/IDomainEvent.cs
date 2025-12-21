namespace TelegramPhotoBot.Domain.Interfaces;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

