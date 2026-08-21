using Application.Messaging;
using Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Infrastructure.Messaging;

internal sealed class WolverineOutbox : IOutbox
{
    private readonly IDbContextOutbox<StockDbContext> _outbox;

    public WolverineOutbox(IDbContextOutbox<StockDbContext> outbox, StockDbContext context)
    {
        if (outbox.DbContext is not null && !ReferenceEquals(outbox.DbContext, context))
            throw new InvalidOperationException(
                "Wolverine's outbox is wrapping a different StockDbContext instance than the " +
                "one the repositories write to, so the state change and the outgoing envelope " +
                "would not commit together. Check that the DbContext is registered through " +
                "AddDbContextWithWolverineIntegration and is scoped.");

        _outbox = outbox;
    }

    public async Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : notnull =>
        await _outbox.PublishAsync(message);

    public Task SaveChangesAndFlushAsync(CancellationToken cancellationToken = default) =>
        _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
}
