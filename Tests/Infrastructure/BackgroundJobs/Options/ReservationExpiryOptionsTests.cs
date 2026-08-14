using System.ComponentModel.DataAnnotations;
using Infrastructure.BackgroundJobs.Options;

namespace Tests.Infrastructure.BackgroundJobs.Options;

public class ReservationExpiryOptionsTests
{
    [Fact]
    public void SectionName_HasExpectedValue()
    {
        ReservationExpiryOptions.SectionName.ShouldBe("ReservationExpiry");
    }

    [Fact]
    public void ExpiryMinutes_DefaultsToThirty()
    {
        var sut = new ReservationExpiryOptions();

        sut.ExpiryMinutes.ShouldBe(30);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(720)]
    [InlineData(1440)]
    public void Validate_WithExpiryMinutesInRange_Succeeds(int minutes)
    {
        var sut = new ReservationExpiryOptions { ExpiryMinutes = minutes };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = Validator.TryValidateObject(
            sut, new ValidationContext(sut), results, validateAllProperties: true);

        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    [InlineData(int.MaxValue)]
    public void Validate_WithExpiryMinutesOutOfRange_Fails(int minutes)
    {
        var sut = new ReservationExpiryOptions { ExpiryMinutes = minutes };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = Validator.TryValidateObject(
            sut, new ValidationContext(sut), results, validateAllProperties: true);

        isValid.ShouldBeFalse();
        results.ShouldNotBeEmpty();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(ReservationExpiryOptions.ExpiryMinutes)));
    }
}
