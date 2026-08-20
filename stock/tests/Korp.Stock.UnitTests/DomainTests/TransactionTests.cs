using Domain.Stocks;
using Domain.Stocks.Transactions.Enums;
using Shouldly;

namespace UnitTests.DomainTests;

public class TransactionTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();
    private static readonly Guid ReferenceId = Guid.CreateVersion7();

    [Fact]
    public void SignedQuantity_ForOpeningBalance_IsPositive()
    {
        var movement = Stock.Init(ProductId, 7).Value.Transactions.Single();

        movement.SignedQuantity.ShouldBe(7);
    }

    [Fact]
    public void SignedQuantity_ForInvoiceOutput_IsNegative()
    {
        var stock = Stock.Init(ProductId, 7).Value;

        var movement = stock.Operate(3, ReferenceId).Value;

        movement.ShouldSatisfyAllConditions(
            () => movement.TransactionType.ShouldBe(TransactionType.InvoiceOutput),
            () => movement.Quantity.ShouldBe(3),
            () => movement.SignedQuantity.ShouldBe(-3));
    }
}
