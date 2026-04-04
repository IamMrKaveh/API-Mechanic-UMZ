using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Shipping.Interfaces;
using SharedKernel.Contracts;

namespace Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingHandler(
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteShippingCommand, ServiceResult>
{
    private readonly IShippingRepository _shippingMethodRepository = shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        DeleteShippingCommand request,
        CancellationToken ct)
    {
        var method = await _shippingMethodRepository.GetByIdAsync(request.Id, ct);
        if (method == null)
            return ServiceResult.NotFound("روش ارسال یافت نشد.");

        try
        {
            method.Delete(request.CurrentUserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        _shippingMethodRepository.Update(method);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogAdminEventAsync(
            "DeleteShippingMethod",
            request.CurrentUserId,
            $"Soft-deleted shipping method ID: {request.Id}",
            _currentUserService.CurrentUser.IpAddress);

        return ServiceResult.Success();
    }
}