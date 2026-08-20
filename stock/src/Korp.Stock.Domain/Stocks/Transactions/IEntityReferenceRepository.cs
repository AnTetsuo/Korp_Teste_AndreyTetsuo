using Domain.Stocks.Transactions.Enums;

namespace Domain.Stocks.Transactions;

public interface IEntityReferenceRepository
{
    Task<EntityReference?> GetAsync(
        EntityType entityType,
        Guid referenceId,
        CancellationToken cancellationToken = default);

    void Add(EntityReference entityReference);
}
