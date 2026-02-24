namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandler : IRequestHandler<DeleteOrderStatusCommand, ServiceResult>
{
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteOrderStatusHandler> _logger;

    public DeleteOrderStatusHandler(
        IOrderStatusRepository orderStatusRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteOrderStatusHandler> logger)
    {
        _orderStatusRepository = orderStatusRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(DeleteOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await _orderStatusRepository.GetByIdAsync(request.Id, cancellationToken);
        if (status == null)
            return ServiceResult.Failure("وضعیت سفارش یافت نشد.", 404);

        var isUsed = await _orderStatusRepository.IsInUseAsync(request.Id, cancellationToken);
        if (isUsed)
            return ServiceResult.Failure("امکان حذف وضعیتی که به سفارشات اختصاص داده شده وجود ندارد.", 400);

        try
        {
            
            status.Delete(request.DeletedByUserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }

        _orderStatusRepository.Update(status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order status {StatusId} deleted by user {UserId}", request.Id, request.DeletedByUserId);

        return ServiceResult.Success();
    }
}