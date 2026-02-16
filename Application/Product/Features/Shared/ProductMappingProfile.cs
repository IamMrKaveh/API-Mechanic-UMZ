using Domain.Attribute.Entities;

namespace Application.Product.Features.Shared;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        // These mappings are lightweight; most DTO construction is done manually
        // in query service or handlers for full control.

        CreateMap<AttributeType, AttributeTypeWithValuesDto>()
            .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.Values));

        CreateMap<AttributeValue, AttributeValueSimpleDto>();
    }
}