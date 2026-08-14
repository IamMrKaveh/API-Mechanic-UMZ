using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Discount.Contracts;
using Application.Inventory.Contracts;
using Application.Order.Features.Commands.CreateOrder;
using Application.Order.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Shipping.Interfaces;
using Domain.User.Interfaces;
using Domain.Variant.Interfaces;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.CreateOrder;

public class CreateOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IDiscountService _discountService = Substitute.For<IDiscountService>(); private readonly IInventoryService _inventoryService = Substitute.For<IInventoryService>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>(); private readonly CreateOrderHandler _sut;

    public CreateOrderHandlerTests()
    {
        _sut = new CreateOrderHandler(
            _orderRepository,
            _userRepository,
            _shippingRepository,
            _variantRepository,
            _discountService,
            _inventoryService,
            _unitOfWork,
            _auditService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyIsNotAGuid_ReturnsValidation()
    {
        var command = new CreateOrderCommand(
            UserId: Guid.NewGuid(),
            ReceiverName: "Receiver",
            UserAddressId: Guid.NewGuid(),
            ShippingId: Guid.NewGuid(),
            DiscountCode: null,
            OrderItems: new List<AdminCreateOrderItemDto> { new() { VariantId = Guid.NewGuid(), Quantity = 1, SellingPrice = 100m } },
            IdempotencyKey: "not-a-guid",
            AdminUserId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _orderRepository.DidNotReceiveWithAnyArgs().ExistsByIdempotencyKeyAsync(default, default);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyUsed_ReturnsConflict()
    {
        var idempotencyKey = Guid.NewGuid();
        _orderRepository.ExistsByIdempotencyKeyAsync(idempotencyKey, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateOrderCommand(
            UserId: Guid.NewGuid(),
            ReceiverName: "Receiver",
            UserAddressId: Guid.NewGuid(),
            ShippingId: Guid.NewGuid(),
            DiscountCode: null,
            OrderItems: new List<AdminCreateOrderItemDto> { new() { VariantId = Guid.NewGuid(), Quantity = 1, SellingPrice = 100m } },
            IdempotencyKey: idempotencyKey.ToString(),
            AdminUserId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        _orderRepository.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
