using Application.Review.Features.Commands.CreateReview;
using Mapster;
using Presentation.Review.Requests;

namespace Presentation.Review.Mapping;

public sealed class ReviewRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateReviewRequest, CreateReviewCommand>()
            .Ignore(dest => dest.UserId);
    }
}