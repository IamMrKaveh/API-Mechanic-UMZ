using Application.Discount.Features.Commands.UpdateDiscount;
using Domain.Discount.Enums;
using FluentValidation.TestHelper;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountValidatorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 07, 23, 10, 0, 0, DateTimeKind.Utc);

    private readonly UpdateDiscountValidator _sut;

    public UpdateDiscountValidatorTests()
    {
        _sut = new UpdateDiscountValidator(new FixedDateTimeProvider(FixedUtcNow));
    }

    private static UpdateDiscountCommand ValidCommand(
        Guid? id = null,
        DiscountType discountType = DiscountType.Percentage,
        decimal value = 10m,
        decimal? maximumDiscountAmount = null,
        int? usageLimit = null,
        DateTime? startsAt = null,
        DateTime? expiresAt = null,
        bool isActive = true) =>
        new(
            id ?? Guid.NewGuid(),
            discountType,
            value,
            maximumDiscountAmount,
            usageLimit,
            startsAt,
            expiresAt,
            isActive);

    // ------------------ Id ------------------

    [Fact]
    public void Validate_WhenIdIsEmpty_HasErrorForId()
    {
        var cmd = ValidCommand(id: Guid.Empty);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("شناسه کد تخفیف الزامی است.");
    }

    [Fact]
    public void Validate_WhenIdIsNotEmpty_HasNoErrorForId()
    {
        var cmd = ValidCommand(id: Guid.NewGuid());

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    // ------------------ Value: general (non FreeShipping) ------------------

    [Theory]
    [InlineData(DiscountType.Percentage, 0)]
    [InlineData(DiscountType.Percentage, -0.01)]
    [InlineData(DiscountType.Percentage, -100)]
    [InlineData(DiscountType.FixedAmount, 0)]
    [InlineData(DiscountType.FixedAmount, -1)]
    [InlineData(DiscountType.FixedAmount, -50000)]
    public void Validate_WhenValueIsNonPositiveAndTypeIsNotFreeShipping_HasErrorForValue(
        DiscountType discountType, decimal value)
    {
        var cmd = ValidCommand(discountType: discountType, value: value);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("مقدار تخفیف باید بزرگتر از صفر باشد.");
    }

    [Theory]
    [InlineData(DiscountType.Percentage, 0.01)]
    [InlineData(DiscountType.Percentage, 50)]
    [InlineData(DiscountType.FixedAmount, 1)]
    [InlineData(DiscountType.FixedAmount, 100000)]
    public void Validate_WhenValueIsPositiveAndTypeIsNotFreeShipping_HasNoErrorForValue(
        DiscountType discountType, decimal value)
    {
        var cmd = ValidCommand(discountType: discountType, value: value);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    // ------------------ Value: FreeShipping bypasses "> 0" ------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.01)]
    public void Validate_WhenTypeIsFreeShipping_ValueRuleIsSkipped(decimal value)
    {
        var cmd = ValidCommand(discountType: DiscountType.FreeShipping, value: value);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    // ------------------ Value: Percentage upper bound (100) ------------------

    [Theory]
    [InlineData(100.01)]
    [InlineData(150)]
    [InlineData(1000)]
    public void Validate_WhenTypeIsPercentageAndValueAbove100_HasErrorForValue(decimal value)
    {
        var cmd = ValidCommand(discountType: DiscountType.Percentage, value: value);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");
    }

    [Fact]
    public void Validate_WhenTypeIsPercentageAndValueEquals100_HasNoErrorForValue()
    {
        var cmd = ValidCommand(discountType: DiscountType.Percentage, value: 100m);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void Validate_WhenTypeIsFixedAmountAndValueAbove100_HasNoErrorForValue()
    {
        // The "<= 100" rule must not apply to non-Percentage types.
        var cmd = ValidCommand(discountType: DiscountType.FixedAmount, value: 500_000m);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    // ------------------ ExpiresAt vs StartsAt / UtcNow ------------------

    [Fact]
    public void Validate_WhenExpiresAtIsNull_HasNoErrorForExpiresAt()
    {
        var cmd = ValidCommand(startsAt: FixedUtcNow, expiresAt: null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_WhenExpiresAtIsBeforeStartsAt_HasErrorForExpiresAt()
    {
        var startsAt = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var expiresAt = new DateTime(2026, 07, 25, 0, 0, 0, DateTimeKind.Utc);

        var cmd = ValidCommand(startsAt: startsAt, expiresAt: expiresAt);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt)
              .WithErrorMessage("تاریخ انقضا باید بعد از تاریخ شروع باشد.");
    }

    [Fact]
    public void Validate_WhenExpiresAtEqualsStartsAt_HasErrorForExpiresAt()
    {
        var moment = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);

        var cmd = ValidCommand(startsAt: moment, expiresAt: moment);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_WhenExpiresAtAfterStartsAt_HasNoErrorForExpiresAt()
    {
        var startsAt = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var expiresAt = new DateTime(2026, 08, 15, 0, 0, 0, DateTimeKind.Utc);

        var cmd = ValidCommand(startsAt: startsAt, expiresAt: expiresAt);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_WhenStartsAtIsNullAndExpiresAtBeforeUtcNow_HasErrorForExpiresAt()
    {
        // With StartsAt null, validator falls back to dateTimeProvider.UtcNow.
        var expiresAt = FixedUtcNow.AddDays(-1);

        var cmd = ValidCommand(startsAt: null, expiresAt: expiresAt);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt)
              .WithErrorMessage("تاریخ انقضا باید بعد از تاریخ شروع باشد.");
    }

    [Fact]
    public void Validate_WhenStartsAtIsNullAndExpiresAtAfterUtcNow_HasNoErrorForExpiresAt()
    {
        var expiresAt = FixedUtcNow.AddDays(1);

        var cmd = ValidCommand(startsAt: null, expiresAt: expiresAt);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    // ------------------ Full valid command ------------------

    [Fact]
    public void Validate_WhenAllFieldsValid_IsValid()
    {
        var cmd = ValidCommand(
            discountType: DiscountType.Percentage,
            value: 25m,
            maximumDiscountAmount: 500_000m,
            usageLimit: 100,
            startsAt: FixedUtcNow,
            expiresAt: FixedUtcNow.AddDays(30),
            isActive: true);

        var result = _sut.TestValidate(cmd);

        result.IsValid.ShouldBeTrue();
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
