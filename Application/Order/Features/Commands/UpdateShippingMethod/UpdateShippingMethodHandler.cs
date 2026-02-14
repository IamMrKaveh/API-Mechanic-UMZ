using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Order.Features.Commands.UpdateShippingMethod;

public class UpdateShippingMethodHandler : IRequestHandler<UpdateShippingMethodCommand, ServiceResult>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateShippingMethodHandler(
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult> Handle(
        UpdateShippingMethodCommand request,
        CancellationToken cancellationToken)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (method == null)
            return ServiceResult.Failure("روش ارسال یافت نشد.", 404);

        if (request.RowVersion != null)
            _shippingMethodRepository.SetOriginalRowVersion(method, Convert.FromBase64String(request.RowVersion));

        // Check duplicate name excluding current
        if (await _shippingMethodRepository.ExistsByNameAsync(request.Name, request.Id, cancellationToken))
            return ServiceResult.Failure("روش ارسال با این نام قبلاً وجود دارد.");

        try
        {
            // Use domain method
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAdminEventAsync(
                "UpdateShippingMethod",
                request.CurrentUserId,
                $"Updated shipping method ID: {request.Id}",
                _currentUserService.IpAddress);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure("رکورد توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}