using System.ComponentModel.DataAnnotations;
using Application.Review.Configuration;
using DaValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Tests.Application.Review.Configuration;

public class ReviewSettingsTests
{
    [Fact]
    public void Defaults_MatchExpectedValues()
    {
        var settings = new ReviewSettings();

        ReviewSettings.SectionName.ShouldBe("ReviewSettings");
        settings.RequirePurchaseVerification.ShouldBeFalse();
        settings.PurchaseReviewWindowDays.ShouldBe(90);
        settings.MaxAdminReplyLength.ShouldBe(1000);
        settings.MaxCommentLength.ShouldBe(1000);
        settings.MinCommentLength.ShouldBe(10);
        settings.MaxTitleLength.ShouldBe(100);
        settings.MaxRejectionReasonLength.ShouldBe(500);
        settings.EnableLikeDislike.ShouldBeFalse();
        settings.RateLimit.ShouldNotBeNull();
    }

    [Fact]
    public void RateLimit_DefaultInstance_IsNotNull()
    {
        new ReviewSettings().RateLimit.ShouldNotBeNull();
    }

    [Fact]
    public void Validate_DefaultSettings_PassesAllDataAnnotations()
    {
        var settings = new ReviewSettings();

        var results = Validate(settings);

        results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3651)]
    public void Validate_PurchaseReviewWindowDaysOutOfRange_Fails(int days)
    {
        var settings = new ReviewSettings { PurchaseReviewWindowDays = days };

        var results = Validate(settings);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(ReviewSettings.PurchaseReviewWindowDays)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Validate_NestedRateLimitOutOfRange_Fails(int value)
    {
        var settings = new ReviewSettings { RateLimit = new ReviewRateLimitSettings { CreateReviewPerMinute = value } };

        var results = Validate(settings);

        results.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_MinCommentLengthZero_Fails()
    {
        var settings = new ReviewSettings { MinCommentLength = 0 };

        var results = Validate(settings);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(ReviewSettings.MinCommentLength)));
    }

    private static List<DaValidationResult> Validate(object instance)
    {
        var results = new List<DaValidationResult>();
        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

        // Recurse into nested RateLimit object (init-only, set via initializer).
        if (instance is ReviewSettings rs && rs.RateLimit is not null)
        {
            var nested = new List<DaValidationResult>();
            Validator.TryValidateObject(rs.RateLimit, new ValidationContext(rs.RateLimit), nested, validateAllProperties: true);
            results.AddRange(nested);
        }

        return results;
    }
}
