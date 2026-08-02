using Application.Review.Features.Commands.BulkOperation;
using Application.Review.Features.Commands.RejectReview;
using Mapster;
using Presentation.Review.Requests;

namespace Presentation.Review.Mapping;

public sealed class AdminReviewMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RejectReviewRequest, RejectReviewCommand>()
            .Map(dest => dest.Reason, src => src.Reason)
            .Ignore(dest => dest.ReviewId);

        config.NewConfig<BulkReviewActionRequest, BulkApproveReviewsCommand>()
            .Map(dest => dest.ReviewIds, src => src.ReviewIds);

        config.NewConfig<BulkRejectReviewsRequest, BulkRejectReviewsCommand>()
            .Map(dest => dest.ReviewIds, src => src.ReviewIds)
            .Map(dest => dest.Reason, src => src.Reason);

        config.NewConfig<BulkDeleteReviewsRequest, BulkDeleteReviewsCommand>()
            .Map(dest => dest.ReviewIds, src => src.ReviewIds)
            .Map(dest => dest.Reason, src => src.Reason);
    }
}
