using System.ComponentModel.DataAnnotations;
using Application.Review.Configuration;
using DaValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Tests.Application.Review.Configuration;

public class ReviewRateLimitSettingsTests
{
    [Fact]
    public void Defaults_MatchExpectedValues()
    {
        var settings = new ReviewRateLimitSettings();

        settings.CreateReviewPerMinute.ShouldBe(5);
        settings.PublicReadsPerMinute.ShouldBe(60);
        settings.AdminActionsPerMinute.ShouldBe(30);
        settings.VotePerMinute.ShouldBe(20);
    }

    [Fact]
    public void Validate_Defaults_Pass()
    {
        Validate(new ReviewRateLimitSettings()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Validate_CreateReviewPerMinuteOutOfRange_Fails(int value)
    {
        var results = Validate(new ReviewRateLimitSettings { CreateReviewPerMinute = value });

        results.ShouldContain(r => r.MemberNames.Contains(nameof(ReviewRateLimitSettings.CreateReviewPerMinute)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public void Validate_PublicReadsPerMinuteOutOfRange_Fails(int value)
    {
        var results = Validate(new ReviewRateLimitSettings { PublicReadsPerMinute = value });

        results.ShouldContain(r => r.MemberNames.Contains(nameof(ReviewRateLimitSettings.PublicReadsPerMinute)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void Validate_BoundaryValues_Pass(int value)
    {
        var results = Validate(new ReviewRateLimitSettings
        {
            CreateReviewPerMinute = value,
            AdminActionsPerMinute = value,
            VotePerMinute = value
        });

        results.ShouldBeEmpty();
    }

    private static List<DaValidationResult> Validate(object instance)
    {
        var results = new List<DaValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
