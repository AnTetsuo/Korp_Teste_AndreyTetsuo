using Domain.Common;
using Domain.Stocks;
using Domain.Stocks.Transactions;
using Domain.Stocks.Transactions.Enums;
using Shouldly;

namespace UnitTests.DomainTests;

public class StockTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();
    private static readonly Guid ReferenceId = Guid.CreateVersion7();

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

        stock.Transactions.Sum(t => t.SignedQuantity).ShouldBe(stock.Quantity);
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

    [Fact]
    public void Operate_WithinBalance_DecrementsAndRecordsOutput()
    {
        var stock = Stock.Init(ProductId, 10).Value;

        var result = stock.Operate(4, ReferenceId);

        var movement = stock.Transactions.Last();

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => stock.Quantity.ShouldBe(6),
            () => stock.Transactions.Count.ShouldBe(2),
            () => movement.TransactionType.ShouldBe(TransactionType.InvoiceOutput),
            () => movement.Quantity.ShouldBe(4),
            () => movement.ReferenceId.ShouldBe(ReferenceId),
            () => movement.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc));
    }

    [Fact]
    public void Operate_ReturnsTheMovementItRecorded()
    {
        var stock = Stock.Init(ProductId, 10).Value;

        var result = stock.Operate(4, ReferenceId);

        result.Value.ShouldBeSameAs(stock.Transactions.Last());
    }

    [Fact]
    public void Operate_BalanceEqualsSignedLedgerSum()
    {
        var stock = Stock.Init(ProductId, 42).Value;

        stock.Operate(5, ReferenceId);
        stock.Operate(7, Guid.CreateVersion7());

        stock.ShouldSatisfyAllConditions(
            () => stock.Quantity.ShouldBe(30),
            () => stock.Transactions.Sum(t => t.SignedQuantity).ShouldBe(stock.Quantity),
            () => stock.Transactions.ShouldAllBe(t => t.Quantity > 0));
    }

    [Fact]
    public void Operate_EntireBalance_LeavesZero()
    {
        var stock = Stock.Init(ProductId, 3).Value;

        var result = stock.Operate(3, ReferenceId);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => stock.Quantity.ShouldBe(0),
            () => stock.Transactions.Sum(t => t.SignedQuantity).ShouldBe(0));
    }

    [Fact]
    public void Operate_RestampsUpdatedAt()
    {
        var stock = Stock.Init(ProductId, 10).Value;
        stock.UpdatedAt = DateTime.UtcNow.AddDays(-1);

        stock.Operate(1, ReferenceId);

        stock.ShouldSatisfyAllConditions(
            () => stock.UpdatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1)),
            () => stock.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc));
    }

    [Fact]
    public void Operate_MoreThanBalance_IsConflict()
    {
        var stock = Stock.Init(ProductId, 2).Value;

        var result = stock.Operate(3, ReferenceId);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Conflict),
            () => result.ErrorMessage.ShouldNotBeNull().ShouldContain("2 available"),
            () => result.ErrorMessage.ShouldNotBeNull().ShouldContain("3 requested"));
    }

    [Fact]
    public void Operate_MoreThanBalance_LeavesStockUntouched()
    {
        var stock = Stock.Init(ProductId, 2).Value;

        stock.Operate(3, ReferenceId);

        stock.ShouldSatisfyAllConditions(
            () => stock.Quantity.ShouldBe(2),
            () => stock.Transactions.ShouldHaveSingleItem());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Operate_WithNonPositiveQuantity_IsInvalid(int quantity)
    {
        var stock = Stock.Init(ProductId, 10).Value;

        var result = stock.Operate(quantity, ReferenceId);

        result.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "quantity"),
            () => stock.Quantity.ShouldBe(10),
            () => stock.Transactions.ShouldHaveSingleItem());
    }

    [Fact]
    public void Operate_WithoutReference_IsInvalid()
    {
        var stock = Stock.Init(ProductId, 10).Value;

        var result = stock.Operate(1, Guid.Empty);

        result.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "entityReferenceId"),
            () => stock.Quantity.ShouldBe(10));
    }

    [Fact]
    public void Operate_FromZeroOpeningBalance_IsConflict()
    {
        var stock = Stock.Init(ProductId, 0).Value;

        var result = stock.Operate(1, ReferenceId);

        result.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Conflict),
            () => stock.Transactions.ShouldBeEmpty());
    }
}
