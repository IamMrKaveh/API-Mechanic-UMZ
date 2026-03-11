using Domain.User.Entities;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutOrderCreationService(
    IOrderRepository orderRepository,
    OrderDomainService orderDomainService,
    IUnitOfWork unitOfWork) : ICheckoutOrderCreationService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly OrderDomainService _orderDomainService = orderDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<Domain.Order.Aggregates.Order>> CreateAsync(
        int userId,
        UserAddress address,
        Domain.Shipping.Aggregates.Shipping shippingMethod,
        string idempotencyKey,
        IReadOnlyList<OrderItemSnapshot> orderItemSnapshots,
        CancellationToken ct)
    {
        var itemsValidation = _orderDomainService.ValidateOrderItems(orderItemSnapshots);
        if (!itemsValidation.IsValid)
            return ServiceResult<Domain.Order.Aggregates.Order>.Failure(itemsValidation.GetErrorsSummary());

        var order = _orderDomainService.PlaceOrder(
            userId,
            address,
            address.ReceiverName,
            shippingMethod,
            idempotencyKey,
            orderItemSnapshots,
            discountResult: null);

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<Domain.Order.Aggregates.Order>.Success(order);
    }
}