using Domain.Stocks.Transactions;

namespace Infrastructure.Persistence.Repositories;

internal sealed class EntityReferenceRepository(StockDbContext context) : IEntityReferenceRepository
{
    public void Add(EntityReference entityReference) => context.EntityReferences.Add(entityReference);
}
