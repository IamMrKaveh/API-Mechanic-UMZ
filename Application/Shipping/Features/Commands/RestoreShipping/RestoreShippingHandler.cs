namespace Application.Shipping.Features.Commands.RestoreShipping;

public class RestoreShippingHandler : IRequestHandler<RestoreShippingCommand, ServiceResult>
{
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public RestoreShippingHandler(
        IShippingRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult> Handle(RestoreShippingCommand request, CancellationToken cancellationToken)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (method == null)
            return ServiceResult.Failure("روش ارسال یافت نشد.", 404);

        
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