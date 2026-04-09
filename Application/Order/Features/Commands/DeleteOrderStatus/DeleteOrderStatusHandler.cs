using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandler(
    IOrderStatusRepository orderStatusRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteOrderStatusHandler> logger) : IRequestHandler<DeleteOrderStatusCommand, ServiceResult>
{
    private readonly IOrderStatusRepository _orderStatusRepository = orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DeleteOrderStatusHandler> _logger = logger;

    public async Task<ServiceResult> Handle(DeleteOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await _orderStatusRepository.GetByIdAsync(request.Id, cancellationToken);
        if (status == null)
            return ServiceResult.NotFound("وضعیت سفارش یافت نشد.");

        var isUsed = await _orderStatusRepository.IsInUseAsync(request.Id, cancellationToken);
        if (isUsed)
            return ServiceResult.Forbidden("امکان حذف وضعیتی که به سفارشات اختصاص داده شده وجود ندارد.");

        try
        {
            status.Delete(request.DeletedByUserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        _orderStatusRepository.Update(status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order status {StatusId} deleted by user {UserId}", request.Id, request.DeletedByUserId);

        return ServiceResult.Success();
    }
}