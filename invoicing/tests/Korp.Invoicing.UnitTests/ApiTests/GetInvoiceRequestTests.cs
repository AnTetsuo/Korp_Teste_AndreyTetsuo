using Api.Endpoints.Invoices.Get;
using Shouldly;

namespace UnitTests.ApiTests;

public class GetInvoiceRequestTests
{
    [Fact]
    public void ToQuery_ParsesTheRouteSegment()
    {
        var invoiceId = Guid.CreateVersion7();

        var query = new GetInvoiceRequest(invoiceId.ToString()).ToQuery();

        query.InvoiceId.ShouldBe(invoiceId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void ToQuery_WithAnUnparseableSegment_YieldsTheEmptyGuid(string? invoiceId)
    {
        var query = new GetInvoiceRequest(invoiceId).ToQuery();

        query.InvoiceId.ShouldBe(Guid.Empty);
    }
}
