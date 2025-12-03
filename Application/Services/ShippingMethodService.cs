namespace Application.Services;

public class ShippingMethodService : IShippingMethodService
{
    private readonly IShippingMethodRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ShippingMethodService> _logger;

    private const string ShippingMethodsCacheKey = "shipping_methods:all";
    private const string ActiveShippingMethodsCacheKey = "shipping_methods:active";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

    public ShippingMethodService(
        IShippingMethodRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer htmlSanitizer,
        ICacheService cacheService,
        ILogger<ShippingMethodService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IEnumerable<ShippingMethodDto>> GetAllAsync(bool includeDeleted = false)
    {
        if (!includeDeleted)
        {
            var cached = await _cacheService.GetAsync<List<ShippingMethodDto>>(ShippingMethodsCacheKey);
            if (cached != null)
            {
                return cached;
            }
        }

        var methods = await _repository.GetAllAsync(includeDeleted);
        var dtos = _mapper.Map<List<ShippingMethodDto>>(methods);

        if (!includeDeleted)
        {
            await _cacheService.SetAsync(ShippingMethodsCacheKey, dtos, CacheExpiry);
        }

        return dtos;
    }

    public async Task<ShippingMethodDto?> GetByIdAsync(int id)
    {
        var method = await _repository.GetByIdAsync(id);
        return method == null ? null : _mapper.Map<ShippingMethodDto>(method);
    }

    public async Task<ServiceResult<ShippingMethodDto>> CreateAsync(ShippingMethodCreateDto dto)
    {
        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());

        if (await _repository.ExistsByNameAsync(sanitizedName))
        {
            return ServiceResult<ShippingMethodDto>.Fail("A shipping method with this name already exists.");
        }

        var method = _mapper.Map<ShippingMethod>(dto);
        method.Name = sanitizedName;
        method.Description = dto.Description != null ? _htmlSanitizer.Sanitize(dto.Description) : null;

        await _repository.AddAsync(method);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateShippingMethodCacheAsync();

        return ServiceResult<ShippingMethodDto>.Ok(_mapper.Map<ShippingMethodDto>(method));
    }

    public async Task<ServiceResult> UpdateAsync(int id, ShippingMethodUpdateDto dto)
    {
        var method = await _repository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("Shipping method not found.");
        }

        if (dto.RowVersion != null)
        {
            _repository.SetOriginalRowVersion(method, dto.RowVersion);
        }

        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());

        if (await _repository.ExistsByNameAsync(sanitizedName, id))
        {
            return ServiceResult.Fail("A shipping method with this name already exists.");
        }

        method.Name = sanitizedName;
        method.Cost = dto.Cost;
        method.Description = dto.Description != null ? _htmlSanitizer.Sanitize(dto.Description) : null;
        method.EstimatedDeliveryTime = dto.EstimatedDeliveryTime;
        method.IsActive = dto.IsActive;
        method.UpdatedAt = DateTime.UtcNow;

        _repository.Update(method);

        try
        {
            await _unitOfWork.SaveChangesAsync();

            await InvalidateShippingMethodCacheAsync();

            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This record was modified by another user.   Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var method = await _repository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("Shipping method not found.");
        }

        method.IsDeleted = true;
        method.DeletedAt = DateTime.UtcNow;
        _repository.Update(method);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateShippingMethodCacheAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RestoreAsync(int id)
    {
        var method = await _repository.GetByIdIncludingDeletedAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("Shipping method not found.");
        }

        method.IsDeleted = false;
        method.DeletedAt = null;
        _repository.Update(method);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateShippingMethodCacheAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetActiveShippingMethodsAsync()
    {
        var cached = await _cacheService.GetAsync<List<ShippingMethodDto>>(ActiveShippingMethodsCacheKey);
        if (cached != null)
        {
            return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(cached);
        }

        var methods = await _repository.GetAllAsync(false);
        var activeMethods = methods.Where(m => m.IsActive).ToList();
        var dtos = _mapper.Map<List<ShippingMethodDto>>(activeMethods);

        await _cacheService.SetAsync(ActiveShippingMethodsCacheKey, dtos, CacheExpiry);

        return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(dtos);
    }

    private async Task InvalidateShippingMethodCacheAsync()
    {
        await _cacheService.ClearAsync(ShippingMethodsCacheKey);
        await _cacheService.ClearAsync(ActiveShippingMethodsCacheKey);

        _logger.LogDebug("Shipping method caches invalidated");
    }
}