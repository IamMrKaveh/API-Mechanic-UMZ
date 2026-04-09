using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Attribute.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Attribute.Interfaces;

public interface IAttributeRepository
{
    Task<IReadOnlyList<AttributeType>> GetAllAttributeTypesAsync(CancellationToken ct = default);

    Task<AttributeType?> GetAttributeTypeByIdAsync(
        AttributeTypeId id,
        CancellationToken ct = default);

    Task<AttributeType?> GetAttributeTypeWithValuesAsync(
        AttributeTypeId id,
        CancellationToken ct = default);

    Task<AttributeValue?> GetAttributeValueByIdAsync(
        AttributeValueId id,
        CancellationToken ct = default);

    Task<IEnumerable<AttributeValue>> GetAttributeValuesByIdsAsync(
        IEnumerable<AttributeValueId> ids,
        CancellationToken ct = default);

    Task<bool> AttributeTypeExistsAsync(
        string name,
        AttributeTypeId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> AttributeValueExistsAsync(
        AttributeTypeId typeId,
        string value,
        AttributeValueId? excludeId = null,
        CancellationToken ct = default);

    Task AddAttributeTypeAsync(
        AttributeType attributeType,
        CancellationToken ct = default);

    Task UpdateAttributeTypeAsync(
        AttributeType attributeType,
        CancellationToken ct = default);

    Task DeleteAttributeTypeAsync(
        AttributeTypeId id,
        UserId? deletedBy = null,
        CancellationToken ct = default);

    Task DeleteAttributeValueAsync(
        AttributeValueId id,
        UserId? deletedBy = null,
        CancellationToken ct = default);
}