using Domain.Common;
using Shouldly;

namespace UnitTests.DomainTests;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessful()
    {
        var result = Result<int>.Success(7);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Status.ShouldBe(ResultStatus.Ok),
            () => result.Value.ShouldBe(7));
    }

    [Fact]
    public void Created_IsSuccessful()
    {
        var result = Result<int>.Created(7);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Status.ShouldBe(ResultStatus.Created),
            () => result.Value.ShouldBe(7));
    }

    [Fact]
    public void ImplicitConversion_ProducesSuccess()
    {
        Result<string> result = "value";

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe("value"));
    }

    [Fact]
    public void Invalid_CarriesValidationErrors()
    {
        var result = Result<int>.Invalid("field", "message");

        var error = result.ValidationErrors.ShouldHaveSingleItem();

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => error.Field.ShouldBe("field"),
            () => error.Message.ShouldBe("message"));
    }

    [Fact]
    public void Invalid_AccumulatesMultipleErrors()
    {
        var result = Result<int>.Invalid(
            new ValidationError("a", "first"),
            new ValidationError("b", "second"));

        result.ValidationErrors.Count.ShouldBe(2);
    }

    [Fact]
    public void Value_OnFailure_Throws()
    {
        var result = Result<int>.NotFound("missing");

        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Theory]
    [InlineData(ResultStatus.NotFound)]
    [InlineData(ResultStatus.Conflict)]
    [InlineData(ResultStatus.Unauthorized)]
    [InlineData(ResultStatus.Forbidden)]
    [InlineData(ResultStatus.Error)]
    public void FailureStatuses_AreNotSuccessful(ResultStatus status)
    {
        Result<int> result = status switch
        {
            ResultStatus.NotFound => Result<int>.NotFound(),
            ResultStatus.Conflict => Result<int>.Conflict(),
            ResultStatus.Unauthorized => Result<int>.Unauthorized(),
            ResultStatus.Forbidden => Result<int>.Forbidden(),
            _ => Result<int>.Error("boom")
        };

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(status),
            () => result.ValidationErrors.ShouldBeEmpty());
    }

    [Fact]
    public void Conflict_CarriesErrorMessage()
    {
        var result = Result<int>.Conflict("already exists");

        result.ErrorMessage.ShouldBe("already exists");
    }
}
