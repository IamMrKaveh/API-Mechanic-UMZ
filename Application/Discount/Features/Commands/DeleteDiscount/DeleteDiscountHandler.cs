namespace Application.Discount.Features.Commands.DeleteDiscount;

public class DeleteDiscountHandler : IRequestHandler<DeleteDiscountCommand, ServiceResult>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDiscountHandler(
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
        DeleteDiscountCommand request,
        CancellationToken ct
        )
    {
        var discount = await _discountRepository.GetByIdAsync(request.Id, ct);
        if (discount == null) return ServiceResult.Failure("یافت نشد.");

        
        discount.Delete(_currentUserService.UserId);

        _discountRepository.Update(discount);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogAdminEventAsync("DeleteDiscount", _currentUserService.UserId ?? 0, $"Deleted discount {request.Id}");
        return ServiceResult.Success();
    }
}