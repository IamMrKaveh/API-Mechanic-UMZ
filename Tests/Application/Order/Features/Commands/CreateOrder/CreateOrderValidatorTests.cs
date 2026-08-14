using Application.Order.Features.Commands.CreateOrder; using Application.Order.Features.Shared;

namespace Tests.Application.Order.Features.Commands.CreateOrder;

public class CreateOrderValidatorTests { private readonly CreateOrderValidator _sut = new();

private static CreateOrderCommand ValidCommand() => new(
    UserId: Guid.NewGuid(),
    ReceiverName: "Receiver",
    UserAddressId: Guid.NewGuid(),
    ShippingId: Guid.NewGuid(),
    DiscountCode: null,
    OrderItems: new List<AdminCreateOrderItemDto> { new() { VariantId = Guid.NewGuid(), Quantity = 1, SellingPrice = 1m } },
    IdempotencyKey: Guid.NewGuid().ToString(),
    AdminUserId: Guid.NewGuid());

[Fact]
public void Validate_WhenAllFieldsValid_IsValid()
{
    _sut.Validate(ValidCommand()).IsValid.ShouldBeTrue();
}

[Fact]
public void Validate_WhenIdempotencyKeyEmpty_HasError()
{
    var cmd = ValidCommand() with { IdempotencyKey = string.Empty };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderCommand.IdempotencyKey));
}

[Fact]
public void Validate_WhenReceiverNameEmpty_HasError()
{
    var cmd = ValidCommand() with { ReceiverName = "" };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderCommand.ReceiverName));
}

[Fact]
public void Validate_WhenOrderItemsEmpty_HasError()
{
    var cmd = ValidCommand() with { OrderItems = new List<AdminCreateOrderItemDto>() };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderCommand.OrderItems));
}

[Theory]
[InlineData(nameof(CreateOrderCommand.UserId))]
[InlineData(nameof(CreateOrderCommand.UserAddressId))]
[InlineData(nameof(CreateOrderCommand.ShippingId))]
[InlineData(nameof(CreateOrderCommand.AdminUserId))]
public void Validate_WhenRequiredGuidIsEmpty_HasErrorForThatField(string fieldName)
{
    var cmd = ValidCommand() switch
    {
        var c when fieldName == nameof(CreateOrderCommand.UserId) => c with { UserId = Guid.Empty },
        var c when fieldName == nameof(CreateOrderCommand.UserAddressId) => c with { UserAddressId = Guid.Empty },
        var c when fieldName == nameof(CreateOrderCommand.ShippingId) => c with { ShippingId = Guid.Empty },
        var c when fieldName == nameof(CreateOrderCommand.AdminUserId) => c with { AdminUserId = Guid.Empty },
        _ => ValidCommand()
    };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == fieldName);
}
}