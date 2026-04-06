using Application.Attribute.Features.Shared;

namespace Application.Attribute.Contracts;

public interface IAttributeQueryService
{
    Task<IReadOnlyList<AttributeTypeDto>> GetAllAttributeTypesAsync(CancellationToken ct = default);

    Task<AttributeTypeDto?> GetAttributeTypeByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AttributeValueDto>> GetAttributeValuesByTypeIdAsync(Guid typeId, CancellationToken ct = default);
}