using Application.Invoices.ListInvoices;
using Shouldly;

namespace UnitTests.ApplicationTests;

public class ListInvoicesResponseTests
{
    private static ListInvoicesResponse Response(int rows, int totalCount) =>
        new([], Page: 1, Rows: rows, TotalCount: totalCount);

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(19, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    public void TotalPages_RoundsUpToCoverThePartialPage(int totalCount, int rows, int expected)
    {
        Response(rows, totalCount).TotalPages.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TotalPages_WhenRowsIsNotPositive_IsZero(int rows)
    {
        Response(rows, totalCount: 42).TotalPages.ShouldBe(0);
    }
}
