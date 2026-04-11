using Application.Attribute.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Mapster;

namespace Application.Attribute.Mapping;

public class AttributeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AttributeType, AttributeTypeDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.DisplayName, src => src.DisplayName)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.Values, src => src.Values.Adapt<List<AttributeValueDto>>());

        config.NewConfig<AttributeValue, AttributeValueDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.AttributeTypeId, src => src.AttributeTypeId.Value)
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.DisplayValue, src => src.DisplayValue)
            .Map(dest => dest.HexCode, src => src.HexCode)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsActive, src => src.IsActive);
    }
}