using Domain.Inventory.Services.Results;

namespace Tests.Domain.Inventory.Services.Results;

public class BatchReservationValidationTests
{
    [Fact]
    public void Valid_HasNoErrors()
    {
        var sut = BatchReservationValidation.Valid();

        sut.IsValid.ShouldBeTrue();
        sut.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Invalid_ExposesProvidedErrors()
    {
        var errors = new[] { "Out of stock", "Unknown variant" };

        var sut = BatchReservationValidation.Invalid(errors);

        sut.IsValid.ShouldBeFalse();
        sut.Errors.ShouldBe(errors);
    }

    [Fact]
    public void Invalid_WithEmptyErrors_IsStillInvalid()
    {
        var sut = BatchReservationValidation.Invalid(Array.Empty<string>());

        sut.IsValid.ShouldBeFalse();
        sut.Errors.ShouldBeEmpty();
    }
}
