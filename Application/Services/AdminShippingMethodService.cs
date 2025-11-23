using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using AutoMapper;
using Domain.Order;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class AdminShippingMethodService : IAdminShippingMethodService
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminShippingMethodService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public AdminShippingMethodService(
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AdminShippingMethodService> logger,
        IHtmlSanitizer htmlSanitizer,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetShippingMethodsAsync(bool includeDeleted = false)
    {
        var methods = await _shippingMethodRepository.GetAllAsync(includeDeleted);
        var dtos = _mapper.Map<IEnumerable<ShippingMethodDto>>(methods);
        return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<ShippingMethodDto?>> GetShippingMethodByIdAsync(int id)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult<ShippingMethodDto?>.Fail("Shipping method not found.");
        }
        var dto = _mapper.Map<ShippingMethodDto>(method);
        return ServiceResult<ShippingMethodDto?>.Ok(dto);
    }

    public async Task<ServiceResult<ShippingMethodDto>> CreateShippingMethodAsync(ShippingMethodCreateDto dto, int currentUserId)
    {
        var method = _mapper.Map<ShippingMethod>(dto);
        method.Name = _htmlSanitizer.Sanitize(dto.Name);
        method.Description = !string.IsNullOrEmpty(dto.Description) ? _htmlSanitizer.Sanitize(dto.Description) : null;

        await _shippingMethodRepository.AddAsync(method);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Shipping method created: {MethodName} (ID: {MethodId}) by User {UserId}", method.Name, method.Id, currentUserId);
        await _auditService.LogAdminEventAsync("CreateShippingMethod", currentUserId, $"Created shipping method: {method.Name}", _currentUserService.IpAddress);

        var resultDto = _mapper.Map<ShippingMethodDto>(method);
        return ServiceResult<ShippingMethodDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateShippingMethodAsync(int id, ShippingMethodUpdateDto dto, int currentUserId)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("Shipping method not found.");
        }

        if (dto.RowVersion != null)
        {
            _shippingMethodRepository.SetOriginalRowVersion(method, dto.RowVersion);
        }

        _mapper.Map(dto, method);
        method.Name = _htmlSanitizer.Sanitize(dto.Name);
        method.Description = !string.IsNullOrEmpty(dto.Description) ? _htmlSanitizer.Sanitize(dto.Description) : null;

        _shippingMethodRepository.Update(method);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Shipping method {MethodId} updated by User {UserId}", id, currentUserId);
            await _auditService.LogAdminEventAsync("UpdateShippingMethod", currentUserId, $"Updated shipping method ID: {id}", _currentUserService.IpAddress);
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("The record was modified by another user. Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> DeleteShippingMethodAsync(int id, int currentUserId)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            return ServiceResult.Fail("Shipping method not found.");
        }

        method.IsDeleted = true;
        method.DeletedAt = DateTime.UtcNow;
        _shippingMethodRepository.Update(method);

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Shipping method {MethodId} soft-deleted by User {UserId}", id, currentUserId);
        await _auditService.LogAdminEventAsync("DeleteShippingMethod", currentUserId, $"Soft-deleted shipping method ID: {id}", _currentUserService.IpAddress);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RestoreShippingMethodAsync(int id, int currentUserId)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(id);
        if (method == null)
        {
            var allMethods = await _shippingMethodRepository.GetAllAsync(true);
            method = allMethods.FirstOrDefault(m => m.Id == id);
            if (method == null)
                return ServiceResult.Fail("Shipping method not found.");
        }

        method.IsDeleted = false;
        method.DeletedAt = null;
        _shippingMethodRepository.Update(method);

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Shipping method {MethodId} restored by User {UserId}", id, currentUserId);
        await _auditService.LogAdminEventAsync("RestoreShippingMethod", currentUserId, $"Restored shipping method ID: {id}", _currentUserService.IpAddress);

        return ServiceResult.Ok();
    }
}