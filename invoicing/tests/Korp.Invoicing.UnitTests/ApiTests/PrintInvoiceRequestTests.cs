using Api.Endpoints.Invoices.Print;
using Shouldly;

namespace UnitTests.ApiTests;

public class PrintInvoiceRequestTests
{
    [Fact]
    public void ToCommand_ParsesTheRouteSegment()
    {
        var invoiceId = Guid.CreateVersion7();

        var command = new PrintInvoiceRequest(invoiceId.ToString()).ToCommand();

        command.InvoiceId.ShouldBe(invoiceId);
    }
}
