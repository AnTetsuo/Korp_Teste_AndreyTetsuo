using Domain.Stocks.Transactions;
using Domain.Stocks.Transactions.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class EntityReferenceRepository(StockDbContext context) : IEntityReferenceRepository
{
    public Task<EntityReference?> GetAsync(
        EntityType entityType,
        Guid referenceId,
        CancellationToken cancellationToken = default) =>
        context.EntityReferences.FirstOrDefaultAsync(
            e => e.EntityType == entityType && e.ReferenceId == referenceId,
            cancellationToken);

    public void Add(EntityReference entityReference) => context.EntityReferences.Add(entityReference);
}
