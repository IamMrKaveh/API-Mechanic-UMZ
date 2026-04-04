using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Shipping.Interfaces;
using SharedKernel.Contracts;

namespace Application.Shipping.Features.Commands.UpdateShipping;

public class UpdateShippingHandler(
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateShippingCommand, ServiceResult>
{
    private readonly IShippingRepository _shippingMethodRepository = shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        UpdateShippingCommand request,
        CancellationToken ct)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, ct);
        if (method == null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        if (request.RowVersion != null)
            _shippingMethodRepository.SetOriginalRowVersion(method, Convert.FromBase64String(request.RowVersion));

        if (await _shippingMethodRepository.ExistsByNameAsync(request.Name, request.Id, ct))
            return ServiceResult.Conflict("روش ارسال با این نام قبلاً وجود دارد.");

        try
        {
            method.Update(
                request.Name,
                Money.FromDecimal(request.Cost),
                request.Description,
                request.EstimatedDeliveryTime,
                request.MinDeliveryDays,
                request.MaxDeliveryDays);

            if (request.IsActive)
                method.Activate();
            else
                method.Deactivate();

            _shippingMethodRepository.Update(method);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogAdminEventAsync(
                "UpdateShippingMethod",
                request.CurrentUserId,
                $"Updated shipping method ID: {request.Id}",
                _currentUserService.CurrentUser.IpAddress,
                _currentUserService.UserAgent);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("رکورد توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}