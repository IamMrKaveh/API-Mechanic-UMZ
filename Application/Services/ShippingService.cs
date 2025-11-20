namespace Application.Services;

public class ShippingService : IShippingService
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingService(
        IShippingMethodRepository shippingMethodRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<ShippingMethodDto>> CreateShippingMethodAsync(ShippingMethodCreateDto dto)
    {
        var shippingMethod = _mapper.Map<ShippingMethod>(dto);
        await _shippingMethodRepository.AddAsync(shippingMethod);
        await _unitOfWork.SaveChangesAsync();
        var resultDto = _mapper.Map<ShippingMethodDto>(shippingMethod);
        return ServiceResult<ShippingMethodDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> DeleteShippingMethodAsync(int id)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("NotFound");
        }
        method.IsDeleted = true;
        method.DeletedAt = DateTime.UtcNow;
        _shippingMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetActiveShippingMethodsAsync()
    {
        var methods = await _shippingMethodRepository.GetActiveShippingMethodsAsync();
        var dtos = _mapper.Map<IEnumerable<ShippingMethodDto>>(methods);
        return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<ShippingMethodDto?>> GetShippingMethodByIdAsync(int id)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult<ShippingMethodDto?>.Fail("NotFound");
        }
        var dto = _mapper.Map<ShippingMethodDto>(method);
        return ServiceResult<ShippingMethodDto?>.Ok(dto);
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetShippingMethodsAsync(bool includeDeleted)
    {
        var methods = await _shippingMethodRepository.GetShippingMethodsAsync(includeDeleted);
        var dtos = _mapper.Map<IEnumerable<ShippingMethodDto>>(methods);
        return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(dtos);
    }

    public async Task<ServiceResult> RestoreShippingMethodAsync(int id)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null || !method.IsDeleted)
        {
            return ServiceResult.Fail("NotFound");
        }
        method.IsDeleted = false;
        method.DeletedAt = null;
        _shippingMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateShippingMethodAsync(int id, ShippingMethodUpdateDto dto)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("NotFound");
        }
        if (dto.RowVersion != null)
        {
            _shippingMethodRepository.SetOriginalRowVersion(method, dto.RowVersion);
        }
        _mapper.Map(dto, method);
        try
        {
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("Concurrency");
        }
    }
}