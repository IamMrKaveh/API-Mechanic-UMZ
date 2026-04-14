using Application.Attribute.Contracts;
using Application.Attribute.Features.Shared;
using Domain.Attribute.ValueObjects;
using Infrastructure.Persistence.Context;
using MapsterMapper;

namespace Infrastructure.Attribute.QueryServices;

public sealed class AttributeQueryService(DBContext context, IMapper mapper) : IAttributeQueryService
{
    public async Task<IEnumerable<AttributeTypeDto>> GetAllAttributeTypesAsync(
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .Where(at => !at.IsDeleted);

        if (!includeInactive)
            query = query.Where(at => at.IsActive);

        var types = await query
            .OrderBy(at => at.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        return mapper.Map<IEnumerable<AttributeTypeDto>>(types);
    }

    public async Task<AttributeTypeDto?> GetAttributeTypeByIdAsync(
        AttributeTypeId attributeTypeId,
        CancellationToken ct = default)
    {
        var attributeType = await context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(at => at.Id == attributeTypeId && !at.IsDeleted, ct);

        return attributeType is null ? null : mapper.Map<AttributeTypeDto>(attributeType);
    }

    public async Task<AttributeTypeWithValuesDto?> GetAttributeTypeWithValuesDtoAsync(
        AttributeTypeId id,
        CancellationToken ct = default)
    {
        var attributeType = await context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(at => at.Id == id && !at.IsDeleted, ct);

        return attributeType is null ? null : mapper.Map<AttributeTypeWithValuesDto>(attributeType);
    }

    public async Task<IReadOnlyList<AttributeValueDto>> GetAttributeValuesByTypeIdAsync(
        AttributeTypeId attributeTypeId,
        CancellationToken ct = default)
    {
        var values = await context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => av.AttributeTypeId == attributeTypeId && !av.IsDeleted && av.IsActive)
            .OrderBy(av => av.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        return mapper.Map<IReadOnlyList<AttributeValueDto>>(values);
    }
}