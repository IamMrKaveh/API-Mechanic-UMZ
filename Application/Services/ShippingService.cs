using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using AutoMapper;

namespace Application.Services;

public class ShippingService : IShippingService
{
    private readonly IAdminShippingMethodService _adminShippingMethodService;
    private readonly IMapper _mapper;

    public ShippingService(IAdminShippingMethodService adminShippingMethodService, IMapper mapper)
    {
        _adminShippingMethodService = adminShippingMethodService;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetActiveShippingMethodsAsync()
    {
        var result = await _adminShippingMethodService.GetShippingMethodsAsync(false);
        if (!result.Success)
        {
            return ServiceResult<IEnumerable<ShippingMethodDto>>.Fail(result.Error ?? "An unknown error occurred.");
        }

        var activeMethods = result.Data?.Where(m => m.IsActive && !m.IsDeleted) ?? Enumerable.Empty<ShippingMethodDto>();
        return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(activeMethods);
    }
}