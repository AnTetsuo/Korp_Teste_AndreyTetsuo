using Api.Endpoints.Invoices.List;
using Application.Invoices.ListInvoices.Enums;
using Domain.Invoices.Enums;
using FluentValidation.TestHelper;

namespace UnitTests.ApiTests;

public class ListInvoicesRequestValidatorTests
{
    private readonly ListInvoicesRequestValidator _validator = new();

    private static ListInvoicesRequest Request(
        long? number = null,
        int rows = 20,
        OrderByOptions? orderBy = null,
        bool? asc = null,
        InvoiceStatus? status = null,
        int? page = null) =>
        new(number, rows, orderBy, asc, status, page);

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(Request());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FullyPopulatedRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(Request(
            number: 7,
            rows: ListInvoicesRequestValidator.MaxRows,
            orderBy: OrderByOptions.Status,
            asc: false,
            status: InvoiceStatus.Closed,
            page: 2));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ListInvoicesRequestValidator.MinRows - 1)]
    [InlineData(ListInvoicesRequestValidator.MaxRows + 1)]
    public void Rows_OutsideRange_HasError(int rows)
    {
        var result = _validator.TestValidate(Request(rows: rows));

        result.ShouldHaveValidationErrorFor(x => x.Rows)
            .WithErrorMessage(
                $"Rows must be between {ListInvoicesRequestValidator.MinRows} and " +
                $"{ListInvoicesRequestValidator.MaxRows}.");
    }

    [Theory]
    [InlineData(ListInvoicesRequestValidator.MinRows)]
    [InlineData(ListInvoicesRequestValidator.MaxRows)]
    public void Rows_AtRangeBoundaries_HasNoError(int rows)
    {
        var result = _validator.TestValidate(Request(rows: rows));

        result.ShouldNotHaveValidationErrorFor(x => x.Rows);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Number_WhenNotPositive_HasError(long number)
    {
        var result = _validator.TestValidate(Request(number: number));

        result.ShouldHaveValidationErrorFor(x => x.Number)
            .WithErrorMessage("Number must be greater than zero.");
    }

    [Fact]
    public void Number_WhenOmitted_HasNoError()
    {
        var result = _validator.TestValidate(Request(number: null));

        result.ShouldNotHaveValidationErrorFor(x => x.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Page_BelowOne_HasError(int page)
    {
        var result = _validator.TestValidate(Request(page: page));

        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Fact]
    public void Page_WhenOmitted_HasNoError()
    {
        var result = _validator.TestValidate(Request(page: null));

        result.ShouldNotHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void OrderBy_OutsideEnum_HasError()
    {
        var result = _validator.TestValidate(Request(orderBy: (OrderByOptions)99));

        result.ShouldHaveValidationErrorFor(x => x.OrderBy);
    }

    [Fact]
    public void Status_OutsideEnum_HasError()
    {
        var result = _validator.TestValidate(Request(status: (InvoiceStatus)99));

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
