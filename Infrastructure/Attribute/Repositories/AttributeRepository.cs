using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;

namespace Infrastructure.Attribute.Repositories;

public class AttributeRepository(DBContext context) : IAttributeRepository
{
    private readonly DBContext _context = context;

    public async Task<AttributeType?> GetAttributeTypeByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .FirstOrDefaultAsync(at => at.Id == id && !at.IsDeleted, ct);
    }

    public async Task<AttributeType?> GetAttributeTypeWithValuesAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(at => at.Id == id && !at.IsDeleted, ct);
    }

    public async Task<AttributeValue?> GetAttributeValueByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .FirstOrDefaultAsync(av => av.Id == id && !av.IsDeleted, ct);
    }

    public async Task<IEnumerable<AttributeValue>> GetAttributeValuesByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => idList.Contains(av.Id) && !av.IsDeleted && av.IsActive)
            .ToListAsync(ct);
    }

    public async Task<bool> AttributeTypeExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _context.AttributeTypes
            .Where(at => at.Name == name && !at.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(at => at.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> AttributeValueExistsAsync(
    int typeId,
    string value,
    int? excludeId = null,
    CancellationToken ct = default)
    {
        var query = _context.AttributeValues
            .Where(av => av.AttributeTypeId == typeId && av.Value == value && !av.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(av => av.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAttributeTypeAsync(
        AttributeType attributeType,
        CancellationToken ct = default)
    {
        await _context.AttributeTypes.AddAsync(attributeType, ct);
    }

    public async Task AddAttributeValueAsync(
        AttributeValue attributeValue,
        CancellationToken ct = default)
    {
        await _context.AttributeValues.AddAsync(attributeValue, ct);
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

    public async Task DeleteAttributeTypeAsync(
        int id,
        int? deletedBy = null,
        CancellationToken ct = default)
    {
        var attributeType = await _context.AttributeTypes
            .Include(at => at.Values)
            .FirstOrDefaultAsync(at => at.Id == id, ct);

        attributeType?.Delete(deletedBy);
    }

    public async Task DeleteAttributeValueAsync(
        int id,
        int? deletedBy = null,
        CancellationToken ct = default)
    {
        var attributeValue = await _context.AttributeValues
            .FirstOrDefaultAsync(av => av.Id == id, ct);

        attributeValue?.Delete(deletedBy);
    }
}