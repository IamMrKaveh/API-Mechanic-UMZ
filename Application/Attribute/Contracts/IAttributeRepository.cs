namespace Application.Attribute.Contracts;

public interface IAttributeRepository
{
    
    Task<AttributeType?> GetAttributeTypeByIdAsync(
        int id,
        CancellationToken ct = default
        );

    Task<AttributeType?> GetAttributeTypeWithValuesAsync(
        int id,
        CancellationToken ct = default
        );

    Task<IEnumerable<AttributeType>> GetAllAttributeTypesAsync(
        bool includeInactive = false,
        CancellationToken ct = default
        );

    Task<bool> AttributeTypeExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken ct = default
        );

    Task AddAttributeTypeAsync(
        AttributeType attributeType,
        CancellationToken ct = default
        );

    void UpdateAttributeType(
        AttributeType attributeType
        );

    Task DeleteAttributeTypeAsync(
        int id,
        int? deletedBy = null,
        CancellationToken ct = default
        );

    
    Task<AttributeValue?> GetAttributeValueByIdAsync(
        int id,
        CancellationToken ct = default
        );

    Task<IEnumerable<AttributeValue>> GetAttributeValuesByTypeIdAsync(
        int typeId,
        CancellationToken ct = default
        );

    Task<IEnumerable<AttributeValue>> GetAttributeValuesByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken ct = default
        );

    Task<bool> AttributeValueExistsAsync(
        int typeId,
        string value,
        int? excludeId = null,
        CancellationToken ct = default
        );

    Task AddAttributeValueAsync(
        AttributeValue attributeValue,
        CancellationToken ct = default
        );

    void UpdateAttributeValue(
        AttributeValue attributeValue
        );

    Task DeleteAttributeValueAsync(
        int id,
        int? deletedBy = null,
        CancellationToken ct = default
        );

    Task<List<AttributeValue>> GetValuesByIdsAsync(
        List<int> allAttrValueIds,
        CancellationToken ct
        );

    Task UpdateAttributeValueAsync(
        AttributeValue attributeValue
        );

    Task UpdateAttributeTypeAsync(
        AttributeType attributeType
        );
}