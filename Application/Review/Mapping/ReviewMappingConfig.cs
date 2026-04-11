using Application.Review.Features.Shared;
using Domain.Review.Aggregates;
using Mapster;

namespace Application.Review.Mapping;

public class ReviewMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductReview, ProductReviewDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.UserId, src => src.UserId.Value)
            .Map(dest => dest.Rating, src => src.Rating.Value)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.Comment, src => src.Comment)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.IsVerifiedPurchase, src => src.IsVerifiedPurchase)
            .Map(dest => dest.LikeCount, src => src.LikeCount)
            .Map(dest => dest.DislikeCount, src => src.DislikeCount)
            .Map(dest => dest.AdminReply, src => src.AdminReply)
            .Map(dest => dest.RepliedAt, src => src.RepliedAt)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Ignore(dest => dest.UserFullName);
    }
}