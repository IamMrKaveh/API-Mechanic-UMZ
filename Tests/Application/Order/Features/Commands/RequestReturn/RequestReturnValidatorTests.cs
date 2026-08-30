using Application.Order.Features.Commands.RequestReturn;

namespace Tests.Application.Order.Features.Commands.RequestReturn;

public class RequestReturnValidatorTests
{
    private readonly RequestReturnValidator _sut = new();

    private static RequestReturnCommand ValidCommand(
        Guid? orderId = null,
        string reason = "کالا آسیب دیده بود.",
        string? rowVersion = "AA==") =>
        new(orderId ?? Guid.NewGuid(), reason, rowVersion);

    // ------------------ OrderId ------------------

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_HasErrorForOrderId()
    {
        var cmd = ValidCommand(orderId: Guid.Empty);

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RequestReturnCommand.OrderId) &&
            e.ErrorMessage == "OrderId is required.");
    }

    [Fact]
    public void Validate_WhenOrderIdIsNotEmpty_HasNoErrorForOrderId()
    {
        var cmd = ValidCommand(orderId: Guid.NewGuid());

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(RequestReturnCommand.OrderId));
    }

    // ------------------ Reason ------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenReasonIsNullOrWhitespace_HasErrorForReason(string? reason)
    {
        var cmd = ValidCommand(reason: reason!);

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RequestReturnCommand.Reason) &&
            e.ErrorMessage == "دلیل درخواست بازگشت الزامی است.");
    }

    [Fact]
    public void Validate_WhenReasonExceeds1000Chars_HasErrorForReason()
    {
        var cmd = ValidCommand(reason: new string('a', 1001));

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RequestReturnCommand.Reason));
    }

    [Fact]
    public void Validate_WhenReasonIsExactly1000Chars_HasNoErrorForReason()
    {
        var cmd = ValidCommand(reason: new string('a', 1000));

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(RequestReturnCommand.Reason));
    }

    // ------------------ RowVersion ------------------

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenRowVersionIsNullOrEmpty_HasErrorForRowVersion(string? rowVersion)
    {
        var cmd = ValidCommand(rowVersion: rowVersion);

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RequestReturnCommand.RowVersion) &&
            e.ErrorMessage == "RowVersion is required.");
    }

    [Fact]
    public void Validate_WhenRowVersionIsProvided_HasNoErrorForRowVersion()
    {
        var cmd = ValidCommand(rowVersion: "AA==");

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(RequestReturnCommand.RowVersion));
    }

    // ------------------ Full valid command ------------------

    [Fact]
    public void Validate_WhenAllFieldsValid_IsValid()
    {
        var cmd = ValidCommand();

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
