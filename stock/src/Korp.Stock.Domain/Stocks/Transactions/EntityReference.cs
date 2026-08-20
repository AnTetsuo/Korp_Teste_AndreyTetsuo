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
        var errors = new ValidationErrors()
            .Require(entityType != EntityType.None, nameof(entityType),
                "A reference must name the kind of entity it points at.")
            .Require(referenceId != Guid.Empty, nameof(referenceId),
                "A reference must point at an external entity.");

        if (errors.Any)
            return Result<EntityReference>.Invalid(errors.ToArray());

        return new EntityReference
        {
            Id = Guid.CreateVersion7(),
            EntityType = entityType,
            ReferenceId = referenceId
        };
    }
}
