using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountHandler : IRequestHandler<UpdateDiscountCommand, ServiceResult>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public UpdateDiscountHandler(
        IDiscountRepository discountRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, cancellationToken);
        if (discount == null) return ServiceResult.Failure("کد تخفیف یافت نشد.");

        // Optimistic Concurrency: تنظیم RowVersion اصلی
        _discountRepository.SetOriginalRowVersion(discount, Convert.FromBase64String(request.RowVersion));

        // به‌روزرسانی از طریق متد Domain
        discount.Update(
            request.Percentage,
            request.MaxDiscountAmount,
            request.MinOrderAmount,
            request.UsageLimit,
            request.IsActive,
            request.ExpiresAt,
            request.StartsAt,
            request.MaxUsagePerUser
        );

        _discountRepository.Update(discount);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAdminEventAsync("UpdateDiscount", _currentUserService.UserId ?? 0, $"Updated discount {discount.Code}");
            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure("این رکورد توسط کاربر دیگری تغییر کرده است.");
        }
    }
}