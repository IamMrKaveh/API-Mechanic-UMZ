namespace Application.Review.Mapping;

public class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<Domain.Review.ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : string.Empty));
    }
}