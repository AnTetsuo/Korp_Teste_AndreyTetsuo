using Domain.Common;
using Domain.Stocks.Transactions;
using Domain.Stocks.Transactions.Enums;
using Shouldly;

namespace UnitTests.DomainTests;

public class EntityReferenceTests
{
    [Fact]
    public void BindReference_WithValidInput_Succeeds()
    {
        var invoiceId = Guid.CreateVersion7();

        var result = EntityReference.BindReference(EntityType.Invoice, invoiceId);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.EntityType.ShouldBe(EntityType.Invoice),
            () => result.Value.ReferenceId.ShouldBe(invoiceId),
            () => result.Value.Id.ShouldNotBe(Guid.Empty));
    }

    [Fact]
    public void BindReference_WithNoneEntityType_IsInvalid()
    {
        var result = EntityReference.BindReference(EntityType.None, Guid.CreateVersion7());

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "entityType"));
    }

    [Fact]
    public void BindReference_WithEmptyReferenceId_IsInvalid()
    {
        var result = EntityReference.BindReference(EntityType.Invoice, Guid.Empty);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "referenceId"));
    }

    [Fact]
    public void BindReference_WithBothInvalid_ReportsBothErrors()
    {
        var result = EntityReference.BindReference(EntityType.None, Guid.Empty);

        result.ShouldSatisfyAllConditions(
            () => result.ValidationErrors.Count.ShouldBe(2),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "entityType"),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "referenceId"));
    }
}
