namespace Tests.ApplicationTest.Discount;

public class CreateDiscountValidatorTests
{
    private readonly CreateDiscountValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPassValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    public void Validate_WithShortCode_ShouldFailValidation(string code)
    {
        var command = new CreateDiscountCommand
        {
            Code = code,
            Percentage = 10
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WithCodeExceedingMaxLength_ShouldFailValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = new string('A', 21),
            Percentage = 10
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("CODE 10")]
    [InlineData("CODE@10")]
    [InlineData("CODE#10")]
    public void Validate_WithInvalidCodeCharacters_ShouldFailValidation(string code)
    {
        var command = new CreateDiscountCommand
        {
            Code = code,
            Percentage = 10
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WithZeroPercentage_ShouldFailValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Percentage);
    }

    [Fact]
    public void Validate_WithPercentageGreaterThan100_ShouldFailValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 101
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Percentage);
    }

    [Fact]
    public void Validate_WithNegativeMaxDiscountAmount_ShouldFailValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10,
            MaxDiscountAmount = -100
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.MaxDiscountAmount);
    }

    [Fact]
    public void Validate_WithNullMaxDiscountAmount_ShouldPassValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10,
            MaxDiscountAmount = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.MaxDiscountAmount);
    }

    [Fact]
    public void Validate_WithExpiresAtBeforeStartsAt_ShouldFailValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10,
            StartsAt = DateTime.UtcNow.AddDays(5),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_WithExpiresAtAfterStartsAt_ShouldPassValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10,
            StartsAt = DateTime.UtcNow.AddDays(1),
            ExpiresAt = DateTime.UtcNow.AddDays(5)
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_WithNegativeUsageLimit_ShouldFailValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10,
            UsageLimit = -1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UsageLimit);
    }

    [Fact]
    public void Validate_WithValidUsageLimit_ShouldPassValidation()
    {
        var command = new CreateDiscountCommand
        {
            Code = "SAVE10",
            Percentage = 10,
            UsageLimit = 100
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.UsageLimit);
    }
}