namespace Infrastructure.Product.Repositories;

public class AttributeRepository : IAttributeRepository
{
    private readonly LedkaContext _context;

    public AttributeRepository(LedkaContext context)
    {
        _context = context;
    }

    // ====================================================================
    // IAttributeRepository — Primary interface methods (with CancellationToken)
    // ====================================================================

    public async Task<AttributeType?> GetAttributeTypeByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .FirstOrDefaultAsync(at => at.Id == id, ct);
    }

    public async Task<AttributeType?> GetAttributeTypeWithValuesAsync(int id, CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(at => at.Id == id, ct);
    }

    public async Task<IEnumerable<AttributeType>> GetAllAttributeTypesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .Where(at => !at.IsDeleted);

        if (!includeInactive)
            query = query.Where(at => at.IsActive);

        return await query
            .OrderBy(at => at.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> AttributeTypeExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.AttributeTypes
            .Where(at => at.Name == name && !at.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(at => at.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAttributeTypeAsync(AttributeType attributeType, CancellationToken ct = default)
    {
        await _context.AttributeTypes.AddAsync(attributeType, ct);
    }

    public void UpdateAttributeType(AttributeType attributeType)
    {
        _context.AttributeTypes.Update(attributeType);
    }

    public async Task DeleteAttributeTypeAsync(int id, int? deletedBy = null, CancellationToken ct = default)
    {
        var attributeType = await _context.AttributeTypes
            .Include(at => at.Values)
            .FirstOrDefaultAsync(at => at.Id == id, ct);

        if (attributeType != null)
        {
            attributeType.Delete(deletedBy);
        }
    }

    public async Task<AttributeValue?> GetAttributeValueByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .FirstOrDefaultAsync(av => av.Id == id, ct);
    }

    public async Task<IEnumerable<AttributeValue>> GetAttributeValuesByTypeIdAsync(int typeId, CancellationToken ct = default)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => av.AttributeTypeId == typeId && !av.IsDeleted)
            .OrderBy(av => av.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AttributeValue>> GetAttributeValuesByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => ids.Contains(av.Id) && !av.IsDeleted && av.IsActive)
            .ToListAsync(ct);
    }

    public async Task<bool> AttributeValueExistsAsync(int typeId, string value, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.AttributeValues
            .Where(av => av.AttributeTypeId == typeId && av.Value == value && !av.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(av => av.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAttributeValueAsync(AttributeValue attributeValue, CancellationToken ct = default)
    {
        await _context.AttributeValues.AddAsync(attributeValue, ct);
    }

    public void UpdateAttributeValue(AttributeValue attributeValue)
    {
        _context.AttributeValues.Update(attributeValue);
    }

    public async Task DeleteAttributeValueAsync(int id, int? deletedBy = null, CancellationToken ct = default)
    {
        var attributeValue = await _context.AttributeValues
            .FirstOrDefaultAsync(av => av.Id == id, ct);

        if (attributeValue != null)
        {
            attributeValue.Delete(deletedBy);
        }
    }

    public async Task<List<AttributeValue>> GetValuesByIdsAsync(List<int> allAttrValueIds, CancellationToken ct)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => allAttrValueIds.Contains(av.Id) && !av.IsDeleted && av.IsActive)
            .ToListAsync(ct);
    }

    public async Task UpdateAttributeValueAsync(AttributeValue attributeValue)
    {
        _context.AttributeValues.Update(attributeValue);
        await Task.CompletedTask;
    }

    public async Task UpdateAttributeTypeAsync(AttributeType attributeType)
    {
        _context.AttributeTypes.Update(attributeType);
        await Task.CompletedTask;
    }

    // ====================================================================
    // Legacy methods (without CancellationToken) — delegate to primary methods
    // ====================================================================

    public async Task<AttributeType?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await GetAttributeTypeByIdAsync(id, ct);
    }

    public async Task<AttributeType?> GetByIdWithValuesAsync(int id, CancellationToken ct = default)
    {
        return await GetAttributeTypeWithValuesAsync(id, ct);
    }

    public async Task<IEnumerable<AttributeType>> GetAllWithValuesAsync(CancellationToken ct = default)
    {
        return await GetAllAttributeTypesAsync(false, ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        return await AttributeTypeExistsAsync(name, excludeId, ct);
    }

    public async Task AddAsync(AttributeType attributeType, CancellationToken ct = default)
    {
        await AddAttributeTypeAsync(attributeType, ct);
    }

    public void Update(AttributeType attributeType)
    {
        UpdateAttributeType(attributeType);
    }

    public async Task<List<AttributeValue>> GetValuesByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => ids.Contains(av.Id) && !av.IsDeleted && av.IsActive)
            .ToListAsync(ct);
    }

    public async Task<AttributeType?> GetAttributeTypeByIdAsync(int id)
    {
        return await GetAttributeTypeByIdAsync(id, default);
    }

    public async Task<bool> AttributeTypeExistsAsync(string name, int? excludeId = null)
    {
        return await AttributeTypeExistsAsync(name, excludeId, default);
    }

    public async Task AddAttributeTypeAsync(AttributeType attributeType)
    {
        await AddAttributeTypeAsync(attributeType, default);
    }

    public async Task DeleteAttributeTypeAsync(AttributeType attributeType)
    {
        attributeType.Delete(null);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AttributeType>> GetAllAttributeTypesAsync()
    {
        return await GetAllAttributeTypesAsync(true, default);
    }

    public async Task<AttributeValue?> GetAttributeValueByIdAsync(int id)
    {
        return await GetAttributeValueByIdAsync(id, default);
    }

    public async Task<bool> AttributeValueExistsAsync(int typeId, string value, int? excludeId = null)
    {
        return await AttributeValueExistsAsync(typeId, value, excludeId, default);
    }

    public async Task AddAttributeValueAsync(AttributeValue attributeValue)
    {
        await AddAttributeValueAsync(attributeValue, default);
    }

    public async Task DeleteAttributeValueAsync(AttributeValue attributeValue)
    {
        attributeValue.Delete();
        await Task.CompletedTask;
    }
}