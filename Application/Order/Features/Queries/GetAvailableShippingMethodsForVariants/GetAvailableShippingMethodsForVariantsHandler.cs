namespace Application.Order.Features.Queries.GetAvailableShippingMethodsForVariants;

public class GetAvailableShippingMethodsForVariantsHandler : IRequestHandler<GetAvailableShippingMethodsForVariantsQuery, ServiceResult<IEnumerable<AvailableShippingMethodDto>>>
{
    private readonly LedkaContext _context;

    public GetAvailableShippingMethodsForVariantsHandler(LedkaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> Handle(GetAvailableShippingMethodsForVariantsQuery request, CancellationToken ct)
    {
        if (request.VariantIds == null || !request.VariantIds.Any())
            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Success(new List<AvailableShippingMethodDto>());

        var variants = await _context.ProductVariants
            .Where(v => request.VariantIds.Contains(v.Id) && !v.IsDeleted)
            .Include(v => v.ProductVariantShippingMethods)
            .AsNoTracking()
            .ToListAsync(ct);

        var allShippingMethods = await _context.ShippingMethods
            .Where(sm => sm.IsActive && !sm.IsDeleted)
            .OrderBy(sm => sm.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        var enabledMethodIdSets = variants
            .Select(v => v.ProductVariantShippingMethods
                .Where(pvsm => pvsm.IsActive)
                .Select(pvsm => pvsm.ShippingMethodId)
                .ToHashSet())
            .ToList();

        HashSet<int>? commonMethodIds = null;
        foreach (var methodIdSet in enabledMethodIdSets)
        {
            if (!methodIdSet.Any()) continue;
            if (commonMethodIds == null)
                commonMethodIds = new HashSet<int>(methodIdSet);
            else
                commonMethodIds.IntersectWith(methodIdSet);
        }

        var availableMethods = commonMethodIds != null
            ? allShippingMethods.Where(sm => commonMethodIds.Contains(sm.Id)).ToList()
            : allShippingMethods;

        var result = availableMethods.Select(method => new AvailableShippingMethodDto
        {
            Id = method.Id,
            Name = method.Name,
            BaseCost = method.BaseCost.Amount,
            TotalMultiplier = 1m, // Simplified logic for variant-only check
            FinalCost = method.BaseCost.Amount,
            IsFreeShipping = method.BaseCost.Amount == 0,
            Description = method.Description,
            EstimatedDeliveryTime = method.GetDeliveryTimeDisplay(),
            MinDeliveryDays = method.MinDeliveryDays,
            MaxDeliveryDays = method.MaxDeliveryDays
        }).ToList();

        return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Success(result);
    }
}