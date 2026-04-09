using Application.Attribute.Features.Shared;
using Domain.Attribute.ValueObjects;

namespace Application.Attribute.Contracts;

public interface IAttributeQueryService
{
    Task<IEnumerable<AttributeTypeDto>> GetAllAttributeTypesAsync(
    bool includeInactive = false,
    CancellationToken ct = default);

    Task<AttributeTypeDto?> GetAttributeTypeByIdAsync(
        AttributeTypeId attributeTypeId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AttributeValueDto>> GetAttributeValuesByTypeIdAsync(
        AttributeTypeId attributeTypeId,
        CancellationToken ct = default);
}