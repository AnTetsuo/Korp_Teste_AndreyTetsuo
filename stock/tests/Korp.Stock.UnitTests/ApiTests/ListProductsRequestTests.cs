using Api.Endpoints.Products.List;
using Application.Products.ListProducts.Enums;
using Shouldly;

namespace UnitTests.ApiTests;

public class ListProductsRequestTests
{
    [Fact]
    public void ToQuery_CopiesEveryFieldInPositionalOrder()
    {
        var request = new ListProductsRequest(
            SearchTerm: "parafuso",
            Rows: 25,
            OrderBy: OrderByOptions.ProductCode,
            Asc: false,
            Active: true,
            Page: 3);

        var query = request.ToQuery();

        query.ShouldSatisfyAllConditions(
            () => query.SearchTerm.ShouldBe("parafuso"),
            () => query.Rows.ShouldBe(25),
            () => query.OrderBy.ShouldBe(OrderByOptions.ProductCode),
            () => query.Asc.ShouldBe(false),
            () => query.Active.ShouldBe(true),
            () => query.Page.ShouldBe(3));
    }

    [Fact]
    public void ToQuery_WhenOptionalsOmitted_KeepsThemNull()
    {
        var request = new ListProductsRequest("", 10, null, null, null, null);

        var query = request.ToQuery();

        query.ShouldSatisfyAllConditions(
            () => query.OrderBy.ShouldBeNull(),
            () => query.Asc.ShouldBeNull(),
            () => query.Active.ShouldBeNull(),
            () => query.Page.ShouldBeNull());
    }
}
