namespace Application.Attribute.Mapping;

public class AttributeMappingProfile : Profile
{
    public AttributeMappingProfile()
    {
        CreateMap<AttributeType, AttributeTypeDto>();

        CreateMap<AttributeType, AttributeTypeWithValuesDto>();

        CreateMap<AttributeValue, AttributeValueDto>()
            .ConstructUsing(src => new AttributeValueDto(
                src.Id,
                src.AttributeType != null ? src.AttributeType.Name : string.Empty,
                src.AttributeType != null ? src.AttributeType.DisplayName : string.Empty,
                src.Value,
                src.DisplayValue,
                src.HexCode))
            .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.AttributeType != null ? src.AttributeType.Name : string.Empty))
            .ForMember(dest => dest.TypeDisplayName, opt => opt.MapFrom(src => src.AttributeType != null ? src.AttributeType.DisplayName : string.Empty));

        CreateMap<AttributeValue, AttributeValueSimpleDto>();
    }
}