using Application.Audit.Contracts;
using Application.Common.Results;
using Application.Discount.Contracts;
using Application.Inventory.Contracts;
using Application.Order.Features.Shared;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using MediatR;

namespace Application.Order.Features.Commands.CreateOrder;

public class CreateOrderHandler(
IOrderRepository orderRepository,
IUserRepository userRepository,
IShippingRepository shippingRepository,
IDiscountService discountService,
IInventoryService inventoryService,
IUnitOfWork unitOfWork,
IAuditService auditService) : IRequestHandler<CreateOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
    CreateOrderCommand request,
    CancellationToken ct)
    {
        var userAddressId = UserAddressId.From(request.UserAddressId);
        var userId = UserId.From(request.UserId);
        var shippingId = ShippingId.From(request.ShippingId);

        if (await orderRepository.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, ct))
            return ServiceResult<int>.Conflict("درخواست تکراری. سفارش قبلاً ثبت شده است.");

        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var userAddress = await userRepository.GetUserAddressAsync(userAddressId.Value.GetHashCode(), ct);
                if (userAddress is null || userAddress.UserId.Value != userId.Value)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult<int>.Validation("آدرس کاربر نامعتبر است.");
                }

                var shipping = await shippingRepository.GetByIdAsync(shippingId.Value.GetHashCode(), ct);
                if (shipping is null || !shipping.IsActive)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult<int>.Validation("روش ارسال انتخاب شده معتبر نیست.");
                }

                var orderItemSnapshots = new List<OrderItemSnapshot>();
                foreach (var item in request.OrderItems)
                {
                    orderItemSnapshots.Add(OrderItemSnapshot.Create(
                        VariantId.From(item.VariantId),
                        Domain.Product.ValueObjects.ProductId.NewId(),
                        Domain.Product.ValueObjects.ProductName.Create("محصول نامشخص"),
                        Domain.Variant.ValueObjects.Sku.Create("SKU-001"),
                        Money.FromDecimal(item.SellingPrice, "IRT"),
                        item.Quantity));
                }

                var totalAmount = orderItemSnapshots.Aggregate(Money.Zero("IRT"), (acc, x) => acc.Add(x.UnitPrice.Multiply(x.Quantity)));
                var orderId = OrderId.NewId();
                var discountAmountToApply = Money.Zero("IRT");
                Domain.Discount.ValueObjects.DiscountCodeId? discountCodeIdToApply = null;

                if (!string.IsNullOrEmpty(request.DiscountCode))
                {
                    var discountServiceResult = await discountService.ApplyDiscountAsync(
                        request.DiscountCode, totalAmount, userId, orderId, ct);

                    if (discountServiceResult.IsSuccess && discountServiceResult.Value != null)
                    {
                        discountAmountToApply = Money.FromDecimal(discountServiceResult.Value.DiscountAmount, "IRT");
                        discountCodeIdToApply = Domain.Discount.ValueObjects.DiscountCodeId.NewId();
                    }
                    else
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult<int>.Validation(discountServiceResult.Error ?? "کد تخفیف نامعتبر است.");
                    }
                }

                var receiverInfo = ReceiverInfo.Create(request.ReceiverName, userAddress.PhoneNumber.Value);
                var deliveryAddress = DeliveryAddress.Create(userAddress.Province, userAddress.City, userAddress.Address, userAddress.PostalCode);
                var shippingCost = shipping.CalculateCost(totalAmount);

                var order = Domain.Order.Aggregates.Order.Place(
                    orderId,
                    userId,
                    receiverInfo,
                    deliveryAddress,
                    shippingCost,
                    discountAmountToApply,
                    discountCodeIdToApply,
                    orderItemSnapshots,
                    Guid.Parse(request.IdempotencyKey));

                await orderRepository.AddAsync(order, ct);
                await unitOfWork.SaveChangesAsync(ct);

                foreach (var oi in order.Items)
                {
                    await inventoryService.AdjustStockAsync(
                        oi.VariantId,
                        Domain.Inventory.ValueObjects.StockQuantity.Create(oi.Quantity),
                        UserId.From(request.AdminUserId),
                        "Admin Created Order",
                        ct);
                }

                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogOrderEventAsync(
                    order.Id,
                    "AdminCreateOrder",
                    IpAddress.Unknown,
                    UserId.From(request.AdminUserId),
                    $"سفارش توسط مدیر ایجاد شد. شماره سفارش: {order.OrderNumber.Value}",
                    ct);

                return ServiceResult<int>.Success(order.Id.Value.GetHashCode());
            }
            catch (DomainException ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult<int>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                await auditService.LogSystemEventAsync("CreateOrderError", ex.Message);
                return ServiceResult<int>.Failure("خطایی در ایجاد سفارش رخ داد.");
            }
        }, ct);
    }
}