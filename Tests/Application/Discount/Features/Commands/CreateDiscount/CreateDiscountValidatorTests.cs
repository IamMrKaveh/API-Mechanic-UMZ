using Application.Discount.Features.Commands.CreateDiscount;
using Domain.Discount.Enums;
using FluentValidation.TestHelper;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountValidatorTests
{
    private readonly CreateDiscountValidator _sut;

    public CreateDiscountValidatorTests()
    {
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 07, 23, 10, 0, 0, DateTimeKind.Utc));
        _sut = new CreateDiscountValidator(dateTimeProvider);
    }

    [Fact]
    public void Percentage_WithZeroValue_ShouldHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "P0", DiscountType.Percentage, 0m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("مقدار تخفیف باید بزرگ‌تر از صفر باشد.");
    }

    [Fact]
    public void Percentage_WithBoundary_001_ShouldNotHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "P1", DiscountType.Percentage, 0.01m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void Percentage_WithBoundary_100_ShouldNotHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "P100", DiscountType.Percentage, 100m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void Percentage_Above_100_ShouldHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "P101", DiscountType.Percentage, 100.01m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");
    }

    [Fact]
    public void FixedAmount_WithZero_ShouldHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "F0", DiscountType.FixedAmount, 0m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("مقدار تخفیف باید بزرگ‌تر از صفر باشد.");
    }

    [Fact]
    public void FixedAmount_WithPositive_ShouldNotHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "F1", DiscountType.FixedAmount, 50000m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void FreeShipping_WithZero_ShouldNotHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "FS", DiscountType.FreeShipping, 0m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public void FreeShipping_WithNonZero_ShouldHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "FS2", DiscountType.FreeShipping, 15m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("در حالت ارسال رایگان، مقدار تخفیف باید صفر باشد.");
    }

    [Fact]
    public void EmptyCode_ShouldHaveError()
    {
        var cmd = new CreateDiscountCommand(
            "", DiscountType.Percentage, 10m, null, null, true, null, null);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Code)
              .WithErrorMessage("کد تخفیف الزامی است.");
    }

    [Fact]
    public void ExpiresBeforeStarts_ShouldHaveError()
    {
        var starts = new DateTime(2026, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var expires = new DateTime(2026, 07, 25, 0, 0, 0, DateTimeKind.Utc);

        var cmd = new CreateDiscountCommand(
            "DATE", DiscountType.Percentage, 10m, null, null, true, starts, expires);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt)
              .WithErrorMessage("تاریخ انقضا باید بعد از تاریخ شروع باشد.");
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
