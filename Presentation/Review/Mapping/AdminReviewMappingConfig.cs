using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;
using Mapster;
using Presentation.Review.Requests;

namespace Presentation.Review.Mapping;

public sealed class AdminReviewMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApproveReviewRequest, ApproveReviewCommand>()
            .Ignore(dest => dest.ReviewId)
            .Ignore(dest => dest.UserId);

        config.NewConfig<DeleteReviewRequest, DeleteReviewCommand>()
            .Ignore(dest => dest.ReviewId)
            .Ignore(dest => dest.UserId);

        config.NewConfig<RejectReviewRequest, RejectReviewCommand>()
            .Map(dest => dest.Reason, src => src.Reason)
            .Ignore(dest => dest.ReviewId)
            .Ignore(dest => dest.UserId);
    }
}