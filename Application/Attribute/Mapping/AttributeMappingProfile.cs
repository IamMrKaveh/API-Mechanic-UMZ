namespace Application.Attribute.Mapping;

public sealed class AttributeMappingProfile : Profile
{
    public AttributeMappingProfile()
    {
        CreateMap<AttributeType, AttributeTypeDto>();
        CreateMap<AttributeType, AttributeTypeWithValuesDto>();
        CreateMap<AttributeValue, AttributeValueDto>()
            .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.AttributeType != null ? src.AttributeType.Name : string.Empty))
            .ForMember(dest => dest.TypeDisplayName, opt => opt.MapFrom(src => src.AttributeType != null ? src.AttributeType.DisplayName : string.Empty));
        CreateMap<AttributeValue, AttributeValueSimpleDto>();
    }
}