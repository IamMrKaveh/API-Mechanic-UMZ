using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Attribute.Repositories;

public sealed class AttributeRepository(DBContext context) : IAttributeRepository
{
    public async Task<AttributeType?> GetAttributeTypeByIdAsync(
        AttributeTypeId id,
        CancellationToken ct = default)
    {
        return await context.AttributeTypes.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<AttributeType?> GetAttributeTypeWithValuesAsync(
        AttributeTypeId id,
        CancellationToken ct = default)
    {
        return await context.AttributeTypes
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<AttributeValue?> GetAttributeValueByIdAsync(
        AttributeValueId id,
        CancellationToken ct = default)
    {
        return await context.AttributeValues
            .Include(v => v.AttributeType)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<IEnumerable<AttributeValue>> GetAttributeValuesByIdsAsync(
        IEnumerable<AttributeValueId> ids,
        CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await context.AttributeValues
            .Where(v => idList.Contains(v.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AttributeType>> GetAllAttributeTypesAsync(
        CancellationToken ct = default)
    {
        var results = await context.AttributeTypes
            .Include(a => a.Values)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<bool> AttributeTypeExistsAsync(
        string name,
        AttributeTypeId? excludeId,
        CancellationToken ct = default)
    {
        var query = context.AttributeTypes.Where(a => a.Name == name);
        if (excludeId is not null)
            query = query.Where(a => a.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> AttributeValueExistsAsync(
        AttributeTypeId typeId,
        string value,
        AttributeValueId? excludeId,
        CancellationToken ct = default)
    {
        var query = context.AttributeValues
            .Where(v => v.AttributeTypeId == typeId && v.Value == value);
        if (excludeId is not null)
            query = query.Where(v => v.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAttributeTypeAsync(AttributeType attributeType, CancellationToken ct = default)
    {
        await context.AttributeTypes.AddAsync(attributeType, ct);
    }

    public Task UpdateAttributeTypeAsync(AttributeType attributeType, CancellationToken ct = default)
    {
        context.AttributeTypes.Update(attributeType);
        return Task.CompletedTask;
    }

    public async Task DeleteAttributeTypeAsync(
        AttributeTypeId id,
        UserId? deletedBy = null,
        CancellationToken ct = default)
    {
        var entity = await context.AttributeTypes.FindAsync([id], ct);
        if (entity is null) return;

        entity.Update(
            entity.Name,
            entity.DisplayName,
            entity.SortOrder,
            false,
            new AlwaysTrueUniquenessChecker());

        context.AttributeTypes.Update(entity);
    }

    public async Task DeleteAttributeValueAsync(
        AttributeValueId id,
        UserId? deletedBy = null,
        CancellationToken ct = default)
    {
        var entity = await context.AttributeValues.FindAsync([id], ct);
        if (entity is null) return;

        entity.Update(entity.Value, entity.DisplayValue, entity.HexCode, entity.SortOrder, false);
        context.AttributeValues.Update(entity);
    }

    private sealed class AlwaysTrueUniquenessChecker : Domain.Attribute.Interfaces.IAttributeTypeUniquenessChecker
    {
        public bool IsUnique(string name, AttributeTypeId? excludeId = null) => true;
    }
}