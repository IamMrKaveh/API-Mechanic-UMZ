using Application.Attribute.Contracts;
using Application.Attribute.Features.Shared;
using Domain.Attribute.ValueObjects;
using Infrastructure.Persistence.Context;
using MapsterMapper;

namespace Infrastructure.Attribute.QueryServices;

public class AttributeQueryService(DBContext context, IMapper mapper) : IAttributeQueryService
{
    private readonly DBContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<IEnumerable<AttributeTypeDto>> GetAllAttributeTypesAsync(
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = _context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .Where(at => !at.IsDeleted);

        if (!includeInactive)
            query = query.Where(at => at.IsActive);

        var types = await query
            .OrderBy(at => at.SortOrder)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<AttributeTypeDto>>(types);
    }

    public Task<AttributeTypeDto?> GetAttributeTypeByIdAsync(AttributeTypeId attributeTypeId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<AttributeTypeWithValuesDto?> GetAttributeTypeWithValuesDtoAsync(
        AttributeTypeId id,
        CancellationToken ct = default)
    {
        var attributeType = await _context.AttributeTypes
            .Include(at => at.Values.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(at => at.Id == id && !at.IsDeleted, ct);

        return attributeType == null ? null : _mapper.Map<AttributeTypeWithValuesDto>(attributeType);
    }

    public async Task<IEnumerable<AttributeValueDto>> GetAttributeValuesByTypeIdAsync(
        AttributeTypeId typeId,
        CancellationToken ct = default)
    {
        var values = await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => av.AttributeTypeId == typeId && !av.IsDeleted)
            .OrderBy(av => av.SortOrder)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<AttributeValueDto>>(values);
    }

    Task<IReadOnlyList<AttributeValueDto>> IAttributeQueryService.GetAttributeValuesByTypeIdAsync(AttributeTypeId attributeTypeId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}