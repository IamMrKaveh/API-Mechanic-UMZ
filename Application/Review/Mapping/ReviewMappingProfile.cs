using Domain.Review.Aggregates;

namespace Application.Review.Mapping;

public class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty));
    }
}