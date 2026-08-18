using Domain.Common;
using Domain.Stocks.Transactions.Enums;

namespace Domain.Stocks.Transactions;

public class EntityReference
{
    public Guid Id { get; set; }
    public Guid ReferenceId { get; set; }
    public EntityType EntityType { get; set; }

    public static Result<EntityReference> BindReference(EntityType entityType, Guid referenceId)
    {
        var errors = new List<ValidationError>();

        if (entityType == EntityType.None)
            errors.Add(new ValidationError(nameof(entityType),
                "A reference must name the kind of entity it points at."));

        if (referenceId == Guid.Empty)
            errors.Add(new ValidationError(nameof(referenceId),
                "A reference must point at an external entity."));

        if (errors.Count > 0)
            return Result<EntityReference>.Invalid([.. errors]);

        return new EntityReference
        {
            Id = Guid.CreateVersion7(),
            EntityType = entityType,
            ReferenceId = referenceId
        };
    }
}
