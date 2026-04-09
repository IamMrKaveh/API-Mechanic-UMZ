using Application.Common.Interfaces;
using Domain.Shipping.Interfaces;

namespace Application.Shipping.Features.Commands.RestoreShipping;

public class RestoreShippingHandler(
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<RestoreShippingCommand, ServiceResult>
{
    private readonly IShippingRepository _shippingMethodRepository = shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        RestoreShippingCommand request,
        CancellationToken ct)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, ct);
        if (method == null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        method.Restore();

        _shippingMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogAdminEventAsync(
            "RestoreShippingMethod",
            request.CurrentUserId,
            $"Restored shipping method ID: {request.Id}",
            _currentUserService.CurrentUser.IpAddress,
            _currentUserService.UserAgent);

        return ServiceResult.Success();
    }
}