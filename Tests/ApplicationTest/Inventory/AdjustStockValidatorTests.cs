namespace Tests.ApplicationTest.Inventory;

public class AdjustStockValidatorTests
{
    private readonly AdjustStockValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPassValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = 10,
            Notes = "افزودن موجودی",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithZeroVariantId_ShouldFailValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 0,
            QuantityChange = 10,
            Notes = "test",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.VariantId);
    }

    [Fact]
    public void Validate_WithNegativeVariantId_ShouldFailValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = -1,
            QuantityChange = 10,
            Notes = "test",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.VariantId);
    }

    [Fact]
    public void Validate_WithZeroQuantityChange_ShouldFailValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = 0,
            Notes = "test",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.QuantityChange);
    }

    [Fact]
    public void Validate_WithPositiveQuantityChange_ShouldPassValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = 5,
            Notes = "افزودن موجودی",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.QuantityChange);
    }

    [Fact]
    public void Validate_WithNegativeQuantityChange_ShouldPassValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = -5,
            Notes = "کاهش موجودی",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.QuantityChange);
    }

    [Fact]
    public void Validate_WithEmptyNotes_ShouldFailValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = 10,
            Notes = "",
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_WithNotesExceedingMaxLength_ShouldFailValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = 10,
            Notes = new string('A', 501),
            UserId = 1
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_WithZeroUserId_ShouldFailValidation()
    {
        var command = new AdjustStockCommand
        {
            VariantId = 1,
            QuantityChange = 10,
            Notes = "test",
            UserId = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}