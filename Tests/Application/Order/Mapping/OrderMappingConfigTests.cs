using Application.Common.Mapping;
using Application.Order.Features.Shared;
using Application.Order.Mapping;
using Mapster;

namespace Tests.Application.Order.Mapping;

public class OrderMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public OrderMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        // Production scans all IRegisters together (MappingDependencyInjection),
        // so GlobalTypeConverter must be present for Money conversions.
        new GlobalTypeConverter().Register(_config);
        new OrderMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Order_ToOrderDto_MapsIdentityMoneyAndStatus()
    {
        var order = new OrderBuilder().Build();

        var dto = _mapper.Map<OrderDto>(order);

        dto.Id.ShouldBe(order.Id.Value);
        dto.OrderNumber.ShouldBe(order.OrderNumber.Value);
        dto.UserId.ShouldBe(order.UserId.Value);
        dto.Status.ShouldBe(order.Status.Value);
        dto.StatusDisplayName.ShouldBe(order.Status.DisplayName);
        dto.SubTotal.ShouldBe(order.SubTotal.Amount);
        dto.ShippingCost.ShouldBe(order.ShippingCost.Amount);
        dto.DiscountAmount.ShouldBe(order.DiscountAmount.Amount);
        dto.FinalAmount.ShouldBe(order.FinalAmount.Amount);
        dto.IsPaid.ShouldBe(order.IsPaid);
        dto.IsCancelled.ShouldBe(order.IsCancelled);
        dto.CreatedAt.ShouldBe(order.CreatedAt);
        dto.UpdatedAt.ShouldBe(order.UpdatedAt);
    }

    [Fact]
    public void Map_Order_ToOrderDto_MapsItemsCollection()
    {
        var order = new OrderBuilder().Build();

        var dto = _mapper.Map<OrderDto>(order);

        dto.Items.Count.ShouldBe(order.OrderItems.Count);
        var first = dto.Items.First();
        var source = order.OrderItems.First();
        first.Id.ShouldBe(source.Id.Value);
        first.VariantId.ShouldBe(source.VariantId.Value);
        first.ProductId.ShouldBe(source.ProductId.Value);
        first.ProductName.ShouldBe(source.ProductName);
        first.UnitPrice.ShouldBe(source.UnitPrice.Amount);
        first.Quantity.ShouldBe(source.Quantity);
        first.TotalPrice.ShouldBe(source.TotalPrice.Amount);
    }

    [Fact]
    public void Map_OrderItem_ToOrderItemDto_MapsSkuAndPrices()
    {
        var order = new OrderBuilder().Build();
        var item = order.OrderItems.First();

        var dto = _mapper.Map<OrderItemDto>(item);

        dto.Sku.ShouldBe(item.Sku);
        dto.TotalPrice.ShouldBe(item.UnitPrice.Amount * item.Quantity);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new OrderMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
