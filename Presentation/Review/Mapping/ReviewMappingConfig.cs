using Application.Review.Features.Commands.CreateReview;
using Mapster;
using Presentation.Review.Requests;

namespace Presentation.Review.Mapping;

public sealed class ReviewMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateReviewRequest, CreateReviewCommand>();
    }
}