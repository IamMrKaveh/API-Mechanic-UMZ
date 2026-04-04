using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Discount.Interfaces;
using SharedKernel.Contracts;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IAuditService auditService) : IRequestHandler<UpdateDiscountCommand, ServiceResult>
{
    private readonly IDiscountRepository _discountRepository = discountRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuditService _auditService = auditService;

    public async Task<ServiceResult> Handle(
        UpdateDiscountCommand request,
        CancellationToken ct)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, ct);
        if (discount == null) return ServiceResult.NotFound("کد تخفیف یافت نشد.");

        _discountRepository.SetOriginalRowVersion(discount, Convert.FromBase64String(request.ConcurrencyToken));

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
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditService.LogAdminEventAsync("UpdateDiscount", _currentUserService.CurrentUser.UserId, $"Updated discount {discount.Code}");
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این رکورد توسط کاربر دیگری تغییر کرده است.");
        }
    }
}