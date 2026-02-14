namespace Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusHandler : IRequestHandler<CreateOrderStatusCommand, ServiceResult<OrderStatusDto>>
{
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderStatusHandler> _logger;

    public CreateOrderStatusHandler(
        IOrderStatusRepository orderStatusRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderStatusHandler> logger)
    {
        _orderStatusRepository = orderStatusRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<OrderStatusDto>> Handle(
        CreateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Check for duplicate name
        var existing = await _orderStatusRepository.GetByNameAsync(request.Name, cancellationToken);
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

        await _orderStatusRepository.AddAsync(status, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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