using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Order.Aggregates;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutOrderCreationServiceTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CheckoutOrderCreationService _sut;

    public CheckoutOrderCreationServiceTests()
    {
        _dateTimeProvider.Today.Returns(new DateOnly(2026, 8, 4));
        _sut = new CheckoutOrderCreationService(_orderRepository, _unitOfWork, _dateTimeProvider);
    }

    private static (ReceiverInfo receiver, DeliveryAddress address) NewAddress() =>
    (
        ReceiverInfo.Create("Ali Rezaei", "09121234567"),
        DeliveryAddress.Create("Tehran", "Tehran", "Valiasr St 123", "1234567890")
    );

    private static IReadOnlyCollection<OrderItemSnapshot> NewItems() =>
        [new OrderItemSnapshotBuilder().WithUnitPrice(100_000m, "IRT").Build()];

    [Fact]
    public async Task CreateAsync_WhenIdempotencyKeyAlreadyExists_ReturnsConflictWithoutCreating()
    {
        _orderRepository
            .ExistsByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var (receiver, address) = NewAddress();
        var result = await _sut.CreateAsync(
            Guid.NewGuid(), receiver, address, NewItems(),
            Money.FromDecimal(50_000m), Money.FromDecimal(0m), null,
            Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        _orderRepository.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateAsync_WhenNewOrder_ReturnsOrderIdNumberAndFinalAmount()
    {
        _orderRepository
            .ExistsByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        global::Domain.Order.Aggregates.Order? captured = null;
        _orderRepository
            .When(r => r.Add(Arg.Any<global::Domain.Order.Aggregates.Order>()))
            .Do(call => captured = call.Arg<global::Domain.Order.Aggregates.Order>());

        var userId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var (receiver, address) = NewAddress();
        var result = await _sut.CreateAsync(
            userId, receiver, address, NewItems(),
            Money.FromDecimal(50_000m), Money.FromDecimal(0m), null,
            idempotencyKey, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.OrderId.ShouldNotBe(Guid.Empty);
        result.Value.OrderNumber.ShouldNotBeNullOrWhiteSpace();
        result.Value.FinalAmount.ShouldBe(150_000m);

        captured.ShouldNotBeNull();
        captured!.UserId.Value.ShouldBe(userId);
        captured.IdempotencyKey.ShouldBe(idempotencyKey);
        captured.ReceiverInfo.ShouldBe(receiver);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDiscountCodeIdIsProvided_PersistsItOnOrder()
    {
        _orderRepository
            .ExistsByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        global::Domain.Order.Aggregates.Order? captured = null;
        _orderRepository
            .When(r => r.Add(Arg.Any<global::Domain.Order.Aggregates.Order>()))
            .Do(call => captured = call.Arg<global::Domain.Order.Aggregates.Order>());

        var discountCodeId = Guid.NewGuid();
        var (receiver, address) = NewAddress();
        var result = await _sut.CreateAsync(
            Guid.NewGuid(), receiver, address, NewItems(),
            Money.FromDecimal(50_000m), Money.FromDecimal(10_000m), discountCodeId,
            Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.FinalAmount.ShouldBe(140_000m);
        captured.ShouldNotBeNull();
        captured!.AppliedDiscountCodeId.ShouldNotBeNull();
        captured.AppliedDiscountCodeId!.Value.ShouldBe(discountCodeId);
    }

    [Fact]
    public async Task CreateAsync_ForwardsIdempotencyKeyToExistenceCheck()
    {
        _orderRepository
            .ExistsByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var idempotencyKey = Guid.NewGuid();
        var ct = new CancellationTokenSource().Token;
        var (receiver, address) = NewAddress();

        await _sut.CreateAsync(
            Guid.NewGuid(), receiver, address, NewItems(),
            Money.FromDecimal(50_000m), Money.FromDecimal(0m), null,
            idempotencyKey, ct);

        await _orderRepository.Received(1).ExistsByIdempotencyKeyAsync(idempotencyKey, ct);
    }
}
