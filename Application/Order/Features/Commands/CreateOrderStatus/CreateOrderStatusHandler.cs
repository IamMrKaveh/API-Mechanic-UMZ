namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusHandler : IRequestHandler<CreateOrderStatusCommand, ServiceResult<OrderStatusDto>>
{
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderStatusHandler> _logger;

    public CreateOrderStatusHandler(
        IOrderStatusRepository orderStatusRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderStatusHandler> logger
        )
    {
        _orderStatusRepository = orderStatusRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<OrderStatusDto>> Handle(
        CreateOrderStatusCommand request,
        CancellationToken ct
        )
    {
        // Check for duplicate name
        var existing = await _orderStatusRepository.GetByNameAsync(request.Name, ct);
        if (existing != null)
            return ServiceResult<OrderStatusDto>.Failure("وضعیت سفارش با این نام قبلاً وجود دارد.");

        // Use domain factory method
        var status = OrderStatus.Create(
            request.Name,
            request.DisplayName,
            request.Icon,
            request.Color,
            request.SortOrder,
            request.AllowCancel,
            request.AllowEdit);

        await _orderStatusRepository.AddAsync(status, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "New order status created: {StatusName} (ID: {StatusId})",
            status.Name, status.Id);

        var dto = new OrderStatusDto
        {
            Id = status.Id,
            Name = status.Name,
            DisplayName = status.DisplayName,
            Icon = status.Icon,
            Color = status.Color,
        };

        return ServiceResult<OrderStatusDto>.Success(dto);
    }
}