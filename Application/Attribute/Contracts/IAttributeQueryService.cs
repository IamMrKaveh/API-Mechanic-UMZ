using Application.Attribute.Features.Shared;
using Application.Product.Features.Shared;

namespace Application.Attribute.Contracts;

public interface IAttributeQueryService
{
    Task<IEnumerable<AttributeTypeDto>> GetAllAttributeTypesAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<AttributeTypeWithValuesDto?> GetAttributeTypeWithValuesDtoAsync(
        int id,
        CancellationToken ct = default);

    Task<IEnumerable<AttributeValueDto>> GetAttributeValuesByTypeIdAsync(
        int typeId,
        CancellationToken ct = default);
}