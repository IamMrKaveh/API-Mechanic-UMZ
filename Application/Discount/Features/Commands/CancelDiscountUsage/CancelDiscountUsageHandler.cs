namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandler : IRequestHandler<CancelDiscountUsageCommand, ServiceResult>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public CancelDiscountUsageHandler(
        IDiscountRepository discountRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService
        )
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult> Handle(
        CancelDiscountUsageCommand request,
        CancellationToken ct
        )
    {
        var discount = await _discountRepository.GetByIdWithUsagesAsync(request.DiscountCodeId, ct);
        if (discount == null)
            return ServiceResult.Failure("کد تخفیف یافت نشد.");

        // لغو استفاده توسط متد Domain (کاهش شمارنده + علامت‌گذاری Usage)
        discount.CancelUsage(request.OrderId);
        _discountRepository.Update(discount);

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogOrderEventAsync(
            request.OrderId,
            "CancelDiscountUsage",
            _currentUserService.UserId ?? 0,
            $"Cancelled discount usage for DiscountCode {discount.Code.Value}");

        return ServiceResult.Success();
    }
}