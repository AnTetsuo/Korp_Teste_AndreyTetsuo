using Domain.Common;
using Domain.Stocks;
using Domain.Stocks.Transactions;
using Domain.Stocks.Transactions.Enums;
using Shouldly;

namespace UnitTests.DomainTests;

public class StockTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();

    [Fact]
    public void Init_WithPositiveQuantity_Succeeds()
    {
        var result = Stock.Init(ProductId, 10);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Quantity.ShouldBe(10),
            () => result.Value.ProductId.ShouldBe(ProductId));
    }

    [Fact]
    public void Init_WithPositiveQuantity_RecordsOpeningTransaction()
    {
        var stock = Stock.Init(ProductId, 10).Value;

        var transaction = stock.Transactions.ShouldHaveSingleItem();

        transaction.ShouldSatisfyAllConditions(
            () => transaction.TransactionType.ShouldBe(TransactionType.Initial),
            () => transaction.Quantity.ShouldBe(10),
            () => transaction.StockId.ShouldBe(stock.Id),
            () => transaction.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc));
    }

    [Fact]
    public void Init_BalanceEqualsLedgerSum()
    {
        var stock = Stock.Init(ProductId, 42).Value;

        stock.Transactions.Sum(t => t.Quantity).ShouldBe(stock.Quantity);
    }

    [Fact]
    public void Init_WithZeroQuantity_RecordsNoTransaction()
    {
        var stock = Stock.Init(ProductId, 0).Value;

        stock.ShouldSatisfyAllConditions(
            () => stock.Quantity.ShouldBe(0),
            () => stock.Transactions.ShouldBeEmpty());
    }

    [Fact]
    public void Init_WithReferenceId_FlowsToTransaction()
    {
        var referenceId = Guid.CreateVersion7();

        var stock = Stock.Init(ProductId, 5, referenceId).Value;

        stock.Transactions.ShouldHaveSingleItem().ReferenceId.ShouldBe(referenceId);
    }

    [Fact]
    public void Init_WithoutReferenceId_LeavesTransactionUnreferenced()
    {
        var stock = Stock.Init(ProductId, 5).Value;

        stock.Transactions.ShouldHaveSingleItem().ReferenceId.ShouldBeNull();
    }

    [Fact]
    public void Init_StampsUtcTimestamps()
    {
        var stock = Stock.Init(ProductId, 1).Value;

        stock.ShouldSatisfyAllConditions(
            () => stock.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc),
            () => stock.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Init_WithNegativeQuantity_IsInvalid(int quantity)
    {
        var result = Stock.Init(ProductId, quantity);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "quantity"));
    }

    [Fact]
    public void Init_WithEmptyProductId_IsInvalid()
    {
        var result = Stock.Init(Guid.Empty, 10);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "productId"));
    }

    [Fact]
    public void Transactions_ExposesReadOnlyView()
    {
        var stock = Stock.Init(ProductId, 5).Value;

        stock.Transactions.ShouldNotBeOfType<List<Transaction>>();
    }
}
