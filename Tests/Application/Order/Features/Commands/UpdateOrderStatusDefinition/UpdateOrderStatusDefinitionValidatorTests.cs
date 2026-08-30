using Application.Order.Features.Commands.UpdateOrderStatusDefinition;

namespace Tests.Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionValidatorTests
{
    private readonly UpdateOrderStatusDefinitionValidator _sut = new();

    private static UpdateOrderStatusDefinitionCommand ValidCommand(
        Guid? id = null,
        string displayName = "پرداخت شده",
        string? icon = null,
        string? color = null,
        int sortOrder = 0,
        bool allowCancel = false,
        bool allowEdit = false,
        string? rowVersion = "AA==") =>
        new(
            id ?? Guid.NewGuid(),
            displayName,
            icon,
            color,
            sortOrder,
            allowCancel,
            allowEdit,
            rowVersion);

    // ------------------ Id ------------------

    [Fact]
    public void Validate_WhenIdIsEmpty_HasErrorForId()
    {
        var cmd = ValidCommand(id: Guid.Empty);

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Id) &&
            e.ErrorMessage == "شناسه وضعیت الزامی است.");
    }

    // ------------------ DisplayName ------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenDisplayNameIsNullOrWhitespace_HasErrorForDisplayName(string? displayName)
    {
        var cmd = ValidCommand(displayName: displayName!);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.DisplayName) &&
            e.ErrorMessage == "نام نمایشی وضعیت الزامی است.");
    }

    [Fact]
    public void Validate_WhenDisplayNameExceeds100Chars_HasErrorForDisplayName()
    {
        var cmd = ValidCommand(displayName: new string('a', 101));

        var result = _sut.Validate(cmd);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.DisplayName) &&
            e.ErrorMessage == "نام نمایشی نمی‌تواند بیش از 100 کاراکتر باشد.");
    }

    [Fact]
    public void Validate_WhenDisplayNameIsExactly100Chars_HasNoErrorForDisplayName()
    {
        var cmd = ValidCommand(displayName: new string('a', 100));

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.DisplayName));
    }

    // ------------------ Icon ------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenIconIsNullOrWhitespace_HasNoErrorForIcon(string? icon)
    {
        // Icon length rule is guarded by When(!IsNullOrWhiteSpace).
        var cmd = ValidCommand(icon: icon);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Icon));
    }

    [Fact]
    public void Validate_WhenIconExceeds100Chars_HasErrorForIcon()
    {
        var cmd = ValidCommand(icon: new string('a', 101));

        var result = _sut.Validate(cmd);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Icon) &&
            e.ErrorMessage == "نام آیکن نمی‌تواند بیش از 100 کاراکتر باشد.");
    }

    [Fact]
    public void Validate_WhenIconIsExactly100Chars_HasNoErrorForIcon()
    {
        var cmd = ValidCommand(icon: new string('a', 100));

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Icon));
    }

    // ------------------ Color ------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenColorIsNullOrWhitespace_HasNoErrorForColor(string? color)
    {
        // Color rules are guarded by When(!IsNullOrWhiteSpace).
        var cmd = ValidCommand(color: color);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Color));
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#fff")]
    [InlineData("#FFAA00")]
    [InlineData("#00ff00")]
    [InlineData("#123abc")]
    public void Validate_WhenColorIsValidHex_HasNoErrorForColor(string color)
    {
        var cmd = ValidCommand(color: color);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Color));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("FFAA00")]      // missing '#'
    [InlineData("#GGG")]        // non-hex chars
    [InlineData("#12")]         // too short
    [InlineData("#1234")]       // 4 chars not allowed by regex
    [InlineData("#12345")]      // 5 chars not allowed by regex
    [InlineData("#1234567")]    // 7 chars not allowed by regex
    [InlineData("#FFAAZZ")]     // 6 chars but non-hex
    public void Validate_WhenColorIsNotValidHex_HasErrorForColor(string color)
    {
        var cmd = ValidCommand(color: color);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Color) &&
            e.ErrorMessage == "کد رنگ باید در قالب Hex معتبر باشد (مثال: #FFAA00).");
    }

    [Fact]
    public void Validate_WhenColorExceeds50Chars_HasErrorForColor()
    {
        // 51 chars, non-hex too: both Color rules apply, we only assert the
        // property is flagged (message can be either length or hex depending on rule order).
        var cmd = ValidCommand(color: new string('a', 51));

        var result = _sut.Validate(cmd);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.Color));
    }

    // ------------------ SortOrder ------------------

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Validate_WhenSortOrderIsNegative_HasErrorForSortOrder(int sortOrder)
    {
        var cmd = ValidCommand(sortOrder: sortOrder);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.SortOrder) &&
            e.ErrorMessage == "ترتیب نمایش نمی‌تواند منفی باشد.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Validate_WhenSortOrderIsNonNegative_HasNoErrorForSortOrder(int sortOrder)
    {
        var cmd = ValidCommand(sortOrder: sortOrder);

        var result = _sut.Validate(cmd);

        result.Errors.ShouldNotContain(e =>
            e.PropertyName == nameof(UpdateOrderStatusDefinitionCommand.SortOrder));
    }

    // ------------------ Full valid command ------------------

    [Fact]
    public void Validate_WhenAllFieldsValid_IsValid()
    {
        var cmd = ValidCommand(
            displayName: "ارسال شده",
            icon: "truck",
            color: "#00AAFF",
            sortOrder: 5,
            allowCancel: true,
            allowEdit: true,
            rowVersion: "AA==");

        var result = _sut.Validate(cmd);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
