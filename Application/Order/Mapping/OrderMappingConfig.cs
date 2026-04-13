using Application.Order.Features.Commands.CancelOrder;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Shared;
using Domain.Order.Entities;
using Mapster;

namespace Application.Order.Mapping;

public class OrderMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Order.Aggregates.Order, OrderDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.OrderNumber, src => src.OrderNumber.Value)
            .Map(dest => dest.UserId, src => src.UserId.Value)
            .Map(dest => dest.Status, src => src.Status.Value)
            .Map(dest => dest.StatusDisplayName, src => src.Status.DisplayName)
            .Map(dest => dest.SubTotal, src => src.SubTotal.Amount)
            .Map(dest => dest.ShippingCost, src => src.ShippingCost.Amount)
            .Map(dest => dest.DiscountAmount, src => src.DiscountAmount.Amount)
            .Map(dest => dest.FinalAmount, src => src.FinalAmount.Amount)
            .Map(dest => dest.IsPaid, src => src.IsPaid)
            .Map(dest => dest.IsCancelled, src => src.IsCancelled)
            .Map(dest => dest.CancellationReason, src => src.CancellationReason)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemDto>>());

        config.NewConfig<OrderItem, OrderItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.ProductName, src => src.ProductName)
            .Map(dest => dest.Sku, src => src.Sku)
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Amount)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.TotalPrice, src => src.TotalPrice.Amount);

        config.NewConfig<CheckoutDto, CheckoutFromCartCommand>()
           .Map(dest => dest.CartId, src => src.CartId)
           .Map(dest => dest.ShippingId, src => src.ShippingId)
           .Map(dest => dest.AddressId, src => src.AddressId)
           .Map(dest => dest.DiscountCode, src => src.DiscountCode)
           .Map(dest => dest.PaymentMethod, src => src.PaymentMethod)
           .Map(dest => dest.IdempotencyKey, src => src.IdempotencyKey)
           .IgnoreNonMapped(true);

        config.NewConfig<CancelOrderDto, CancelOrderCommand>()
            .Map(dest => dest.Reason, src => src.Reason)
            .IgnoreNonMapped(true);
    }
}