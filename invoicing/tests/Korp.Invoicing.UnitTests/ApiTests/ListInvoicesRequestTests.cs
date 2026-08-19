using Api.Endpoints.Invoices.List;
using Application.Invoices.ListInvoices.Enums;
using Domain.Invoices.Enums;
using Shouldly;

namespace UnitTests.ApiTests;

public class ListInvoicesRequestTests
{
    [Fact]
    public void ToQuery_CopiesEveryFieldInPositionalOrder()
    {
        var request = new ListInvoicesRequest(
            Number: 42,
            Rows: 25,
            OrderBy: OrderByOptions.CreatedAt,
            Asc: true,
            Status: InvoiceStatus.Processing,
            Page: 3);

        var query = request.ToQuery();

        query.ShouldSatisfyAllConditions(
            () => query.Number.ShouldBe(42),
            () => query.Rows.ShouldBe(25),
            () => query.OrderBy.ShouldBe(OrderByOptions.CreatedAt),
            () => query.Asc.ShouldBe(true),
            () => query.Status.ShouldBe(InvoiceStatus.Processing),
            () => query.Page.ShouldBe(3));
    }

    [Fact]
    public void ToQuery_WhenOptionalsOmitted_KeepsThemNull()
    {
        var request = new ListInvoicesRequest(null, 10, null, null, null, null);

        var query = request.ToQuery();

        query.ShouldSatisfyAllConditions(
            () => query.Number.ShouldBeNull(),
            () => query.Rows.ShouldBe(10),
            () => query.OrderBy.ShouldBeNull(),
            () => query.Asc.ShouldBeNull(),
            () => query.Status.ShouldBeNull(),
            () => query.Page.ShouldBeNull());
    }
}
