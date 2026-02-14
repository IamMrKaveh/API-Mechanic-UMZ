namespace Infrastructure.Product.Repositories;

public class AttributeRepository : IAttributeRepository
{
    private readonly LedkaContext _context;

    public AttributeRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<AttributeType?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .FirstOrDefaultAsync(at => at.Id == id, ct);
    }

    public async Task<AttributeType?> GetByIdWithValuesAsync(int id, CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(at => at.Id == id, ct);
    }

    public async Task<IEnumerable<AttributeType>> GetAllWithValuesAsync(CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .Where(at => !at.IsDeleted)
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .OrderBy(at => at.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.AttributeTypes
            .Where(at => at.Name == name && !at.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(at => at.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(AttributeType attributeType, CancellationToken ct = default)
    {
        await _context.AttributeTypes.AddAsync(attributeType, ct);
    }

    public void Update(AttributeType attributeType)
    {
        _context.AttributeTypes.Update(attributeType);
    }

    public async Task<List<AttributeValue>> GetValuesByIdsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => ids.Contains(av.Id) && !av.IsDeleted && av.IsActive)
            .ToListAsync(ct);
    }

    public async Task<AttributeType?> GetAttributeTypeByIdAsync(int id)
    {
        return await _context.AttributeTypes.FindAsync(id);
    }

    public async Task<bool> AttributeTypeExistsAsync(string name, int? excludeId = null)
    {
        var query = _context.AttributeTypes.Where(x => x.Name == name);
        if (excludeId.HasValue) query = query.Where(x => x.Id != excludeId);
        return await query.AnyAsync();
    }

    public async Task AddAttributeTypeAsync(AttributeType attributeType)
    {
        await _context.AttributeTypes.AddAsync(attributeType);
    }

    public void UpdateAttributeTypeAsync(AttributeType attributeType)
    {
        _context.AttributeTypes.Update(attributeType);
    }

    public async Task DeleteAttributeTypeAsync(AttributeType attributeType)
    {
        attributeType.Delete(null);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AttributeType>> GetAllAttributeTypesAsync()
    {
        return await _context.AttributeTypes.ToListAsync();
    }

    public async Task<AttributeValue?> GetAttributeValueByIdAsync(int id)
    {
        return await _context.AttributeValues.Include(x => x.AttributeType).FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> AttributeValueExistsAsync(int typeId, string value, int? excludeId = null)
    {
        var query = _context.AttributeValues.Where(x => x.AttributeTypeId == typeId && x.Value == value);
        if (excludeId.HasValue) query = query.Where(x => x.Id != excludeId);
        return await query.AnyAsync();
    }

    public async Task AddAttributeValueAsync(AttributeValue attributeValue)
    {
        await _context.AttributeValues.AddAsync(attributeValue);
    }

    public async Task UpdateAttributeValueAsync(AttributeValue attributeValue)
    {
        _context.AttributeValues.Update(attributeValue);
        await Task.CompletedTask;
    }

    public async Task DeleteAttributeValueAsync(AttributeValue attributeValue)
    {
        attributeValue.Delete();
        await Task.CompletedTask;
    }

    public Task<AttributeType?> GetAttributeTypeByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<AttributeType?> GetAttributeTypeWithValuesAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AttributeType>> GetAllAttributeTypesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AttributeTypeExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAttributeTypeAsync(AttributeType attributeType, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void UpdateAttributeType(AttributeType attributeType)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAttributeTypeAsync(int id, int? deletedBy = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<AttributeValue?> GetAttributeValueByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AttributeValue>> GetAttributeValuesByTypeIdAsync(int typeId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AttributeValue>> GetAttributeValuesByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AttributeValueExistsAsync(int typeId, string value, int? excludeId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAttributeValueAsync(AttributeValue attributeValue, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void UpdateAttributeValue(AttributeValue attributeValue)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAttributeValueAsync(int id, int? deletedBy = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<AttributeValue>> GetValuesByIdsAsync(List<int> allAttrValueIds, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    Task IAttributeRepository.UpdateAttributeTypeAsync(AttributeType attributeType)
    {
        throw new NotImplementedException();
    }
}