using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Order.Features.Commands.RestoreShippingMethod;

public class RestoreShippingMethodHandler : IRequestHandler<RestoreShippingMethodCommand, ServiceResult>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public RestoreShippingMethodHandler(
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

    public async Task<ServiceResult> Handle(RestoreShippingMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (method == null)
            return ServiceResult.Failure("روش ارسال یافت نشد.", 404);

        // Use domain method
        method.Restore();

        _shippingMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminEventAsync(
            "RestoreShippingMethod",
            request.CurrentUserId,
            $"Restored shipping method ID: {request.Id}",
            _currentUserService.IpAddress);

        return ServiceResult.Success();
    }
}