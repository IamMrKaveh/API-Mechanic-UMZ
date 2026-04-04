using Application.Audit.Contracts;
using Application.Common.Results;
using Application.Discount.Contracts;
using Application.Inventory.Contracts;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Discount.Results;
using Domain.Order.Interfaces;
using Domain.Shipping.Interfaces;
using Domain.User.Interfaces;

namespace Application.Order.Features.Commands.CreateOrder;

public class CreateOrderHandler(
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    IShippingRepository shippingRepository,
    IDiscountService discountService,
    IInventoryService inventoryService,
    IUnitOfWork unitOfWork,
    OrderDomainService orderDomainService,
    IAuditService auditService,
    ILogger<CreateOrderHandler> logger) : IRequestHandler<CreateOrderCommand, ServiceResult<int>>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IShippingRepository _shippingRepository = shippingRepository;
    private readonly IDiscountService _discountService = discountService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OrderDomainService _orderDomainService = orderDomainService;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<CreateOrderHandler> _logger = logger;

    public async Task<ServiceResult<int>> Handle(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        if (await _orderRepository.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, ct))
            return ServiceResult<int>.Conflict("درخواست تکراری. سفارش قبلاً ثبت شده است.");

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            try
            {
                var userAddress = await _userRepository.GetUserAddressAsync(
                    request.Dto.UserAddressId, ct);
                if (userAddress == null || userAddress.UserId != request.Dto.UserId)
                    return ServiceResult<int>.Validation("آدرس کاربر نامعتبر است.");

                var shipping = await _shippingRepository.GetByIdAsync(
                    request.Dto.ShippingId, ct);
                if (shipping == null || !shipping.IsActive)
                    return ServiceResult<int>.Validation("روش ارسال انتخاب شده معتبر نیست.");

                var orderItemSnapshots = new List<OrderItemSnapshot>();

                foreach (var itemDto in request.Dto.OrderItems)
                {
                    orderItemSnapshots.Add(OrderItemSnapshot.Create(
                        variantId: itemDto.VariantId,
                        productId: 0,
                        productName: "محصول",
                        variantSku: null,
                        variantAttributes: null,
                        quantity: itemDto.Quantity,
                        purchasePrice: 0,
                        sellingPrice: itemDto.SellingPrice));
                }

                DiscountApplicationResult? discountResult = null;
                if (!string.IsNullOrEmpty(request.Dto.DiscountCode))
                {
                    var totalAmount = orderItemSnapshots.Sum(x => x.SellingPrice.Amount * x.Quantity);
                    var discountServiceResult = await _discountService.ValidateAndApplyDiscountAsync(
                        request.Dto.DiscountCode, totalAmount, request.Dto.UserId);

                    if (discountServiceResult.IsSuccess && discountServiceResult.Value != null)
                    {
                        discountResult = DiscountApplicationResult.Success(
                            discountServiceResult.Value.DiscountCodeId,
                            Money.FromDecimal(discountServiceResult.Value.DiscountAmount));
                    }
                }

                var order = _orderDomainService.PlaceOrder(
                    request.Dto.UserId,
                    userAddress,
                    request.Dto.ReceiverName,
                    shipping,
                    request.IdempotencyKey,
                    orderItemSnapshots,
                    discountResult);

                await _orderRepository.AddAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                foreach (var oi in order.OrderItems)
                {
                    await _inventoryService.LogTransactionAsync(
                        oi.VariantId,
                        "Sale",
                        -oi.Quantity,
                        oi.Id,
                        order.UserId,
                        "Admin Created Order",
                        $"ORDER-{order.Id}",
                        null,
                        false,
                        ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);

                await _auditService.LogOrderEventAsync(
                    order.Id,
                    "AdminCreateOrder",
                    request.AdminUserId,
                    $"سفارش توسط مدیر ایجاد شد. شماره سفارش: {order.OrderNumber.Value}");

                _logger.LogInformation(
                    "Admin {AdminId} created order {OrderId} for user {UserId}",
                    request.AdminUserId, order.Id, request.Dto.UserId);

                return ServiceResult<int>.Success(order.Id);
            }
            catch (DomainException ex)
            {
                return ServiceResult<int>.Unexpected(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin order");
                return ServiceResult<int>.Unexpected("خطایی در ایجاد سفارش رخ داد.");
            }
        }, ct);
    }
}