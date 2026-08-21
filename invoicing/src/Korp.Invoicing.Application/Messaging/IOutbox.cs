namespace Application.Messaging;

public interface IOutbox
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : notnull;

    Task ScheduleAsync<TMessage>(
        TMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
        where TMessage : notnull;

    Task SaveChangesAndFlushAsync(CancellationToken cancellationToken = default);
}
