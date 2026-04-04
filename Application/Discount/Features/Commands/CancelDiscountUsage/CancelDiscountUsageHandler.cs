using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Discount.Interfaces;
using SharedKernel.Contracts;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<CancelDiscountUsageCommand, ServiceResult>
{
    private readonly IDiscountRepository _discountRepository = discountRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        CancelDiscountUsageCommand request,
        CancellationToken ct)
    {
        var discount = await _discountRepository.GetByIdWithUsagesAsync(request.DiscountCodeId, ct);
        if (discount == null)
            return ServiceResult.NotFound("کد تخفیف یافت نشد.");

        discount.CancelUsage(request.OrderId);
        _discountRepository.Update(discount);

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogOrderEventAsync(
            request.OrderId,
            "CancelDiscountUsage",
            _currentUserService.CurrentUser.UserId,
            $"Cancelled discount usage for DiscountCode {discount.Code.Value}");

        return ServiceResult.Success();
    }
}