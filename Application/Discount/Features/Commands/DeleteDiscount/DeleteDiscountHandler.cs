using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Discount.Interfaces;
using SharedKernel.Contracts;

namespace Application.Discount.Features.Commands.DeleteDiscount;

public class DeleteDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteDiscountCommand, ServiceResult>
{
    private readonly IDiscountRepository _discountRepository = discountRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        DeleteDiscountCommand request,
        CancellationToken ct)
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, ct);
        if (discount == null)
            return ServiceResult.NotFound("کد تخفیف یافت نشد.");

        discount.Delete(_currentUserService.CurrentUser.UserId);

        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogAdminEventAsync(
            "DeleteDiscount",
            _currentUserService.CurrentUser.UserId,
            $"Deleted discount {request.Id}");
        return ServiceResult.Success();
    }
}