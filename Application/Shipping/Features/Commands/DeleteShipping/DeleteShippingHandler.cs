namespace Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingHandler : IRequestHandler<DeleteShippingCommand, ServiceResult>
{
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteShippingHandler(
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

    public async Task<ServiceResult> Handle(DeleteShippingCommand request, CancellationToken cancellationToken)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (method == null)
            return ServiceResult.Failure("روش ارسال یافت نشد.", 404);

        try
        {
            // Use domain method which enforces business rules
            method.Delete(request.CurrentUserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }

        _shippingMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminEventAsync(
            "DeleteShippingMethod",
            request.CurrentUserId,
            $"Soft-deleted shipping method ID: {request.Id}",
            _currentUserService.IpAddress);

        return ServiceResult.Success();
    }
}