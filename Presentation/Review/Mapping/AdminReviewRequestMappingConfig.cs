using Application.Review.Features.Commands.ApproveReview;
using Mapster;
using Presentation.Review.Requests;

namespace Presentation.Review.Mapping;

public sealed class AdminReviewRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApproveReviewRequest, ApproveReviewCommand>()
            .Ignore(dest => dest.ReviewId)
            .Ignore(dest => dest.UserId);
    }
}