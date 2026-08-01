using Application.Review.Configuration;

namespace Infrastructure.Common.DependencyInjection;

public static class ReviewConfigurationExtensions
{
    public static IServiceCollection AddReviewSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ReviewSettings>()
            .Bind(configuration.GetSection(ReviewSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                s => s.MinCommentLength <= s.MaxCommentLength,
                "ReviewSettings.MinCommentLength must be less than or equal to MaxCommentLength.")
            .ValidateOnStart();

        return services;
    }
}
