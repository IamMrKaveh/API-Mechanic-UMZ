using Application.Common.Exceptions;
using Application.Order.Features.Shared;
using Domain.Order.Aggregates;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using Infrastructure.Order.Services.Strategies;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services.Strategies;

public class WalletCheckoutPaymentStrategyTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IPaymentTransactionRepository _paymentTransactionRepository = Substitute.For<IPaymentTransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly WalletCheckoutPaymentStrategy _sut;

    private static readonly DateTime FixedNow = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    public WalletCheckoutPaymentStrategyTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new WalletCheckoutPaymentStrategy(
            _walletRepository,
            _orderRepository,
            _paymentTransactionRepository,
            _unitOfWork,
            _dateTimeProvider,
            _auditService);
    }

    private static CheckoutResultDto NewOrderResult(Guid? orderId = null, decimal finalAmount = 150_000m) => new()
    {
        OrderId = orderId ?? Guid.NewGuid(),
        OrderNumber = "ON-2002",
        FinalAmount = finalAmount
    };

    private static global::Domain.Order.Aggregates.Order NewOrder(UserId? userId = null)
    {
        var order = new OrderBuilder().WithUserId(userId ?? UserId.NewId()).Build();
        order.ClearDomainEvents();
        return order;
    }

    private static global::Domain.Wallet.Aggregates.Wallet NewFundedWallet(UserId userId, decimal amount = 500_000m)
    {
        var wallet = new WalletBuilder().WithOwnerId(userId).Build();
        wallet.Credit(Money.Create(amount), "seed", Guid.NewGuid().ToString("N"));
        wallet.ClearDomainEvents();
        return wallet;
    }

    [Fact]
    public void Code_IsWallet()
    {
        _sut.Code.ShouldBe("wallet");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAmountIsZero_SettlesFreeOrder()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        var orderResult = NewOrderResult(order.Id.Value, finalAmount: 0m);
        var idempotencyKey = Guid.NewGuid();

        var result = await _sut.ExecuteAsync(
            orderResult, order.Id, userId, Money.FromDecimal(0m),
            "127.0.0.1", null, idempotencyKey, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsPaid.ShouldBeTrue();
        result.Value.PaymentAuthority.ShouldBe($"FREE-{idempotencyKey:N}");
        result.Value.PaymentTransactionId.ShouldNotBeNull();
        result.Value.PaymentMethodCode.ShouldBe("wallet");
        order.IsPaid.ShouldBeTrue();
        await _walletRepository.DidNotReceiveWithAnyArgs().GetByUserIdForUpdateAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var userId = UserId.NewId();
        var wallet = NewFundedWallet(userId);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Order.Aggregates.Order?)null);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(), OrderId.NewId(), userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWalletDoesNotExist_ReturnsNotFound()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Wallet.Aggregates.Wallet?)null);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBalanceIsInsufficient_ReturnsFailure()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var wallet = NewFundedWallet(userId, amount: 10_000m);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        order.IsPaid.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenWalletIsInactive_ReturnsFailure()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var wallet = NewFundedWallet(userId);
        wallet.Freeze("fraud", UserId.NewId());
        wallet.ClearDomainEvents();
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldBe("کیف پول شما غیرفعال است.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaymentSucceeds_DebitsWalletAndMarksOrderPaid()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var orderResult = NewOrderResult(order.Id.Value);
        var wallet = NewFundedWallet(userId);
        var idempotencyKey = Guid.NewGuid();
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _walletRepository.GetByUserIdForUpdateAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        global::Domain.Payment.Aggregates.PaymentTransaction? captured = null;
        await _paymentTransactionRepository.AddAsync(
            Arg.Do<global::Domain.Payment.Aggregates.PaymentTransaction>(t => captured = t),
            Arg.Any<CancellationToken>());

        var result = await _sut.ExecuteAsync(
            orderResult, order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, idempotencyKey, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsPaid.ShouldBeTrue();
        result.Value.PaymentAuthority.ShouldBe($"WALLET-{idempotencyKey:N}");
        result.Value.PaymentTransactionId.ShouldBe(captured!.Id.Value);
        result.Value.PaymentMethodCode.ShouldBe("wallet");
        order.IsPaid.ShouldBeTrue();
        _walletRepository.Received(1).Update(wallet);
        _orderRepository.Received(1).Update(order);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyProcessed_SkipsDebitAndSettles()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsPaid.ShouldBeTrue();
        await _walletRepository.DidNotReceiveWithAnyArgs().GetByUserIdForUpdateAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConcurrencyConflict_ReturnsConflictAndLogs()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new ConcurrencyException("conflict"));
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletCheckoutConcurrencyConflict",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderAlreadyPaid_ReturnsPaidResult()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var paidTx = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(userId)
            .Build();
        paidTx.MarkAsSuccess(111L, FixedNow);
        paidTx.ClearDomainEvents();
        order.MarkAsPaid(paidTx.Id);
        order.ClearDomainEvents();
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsPaid.ShouldBeTrue();
        result.Value.PaymentTransactionId.ShouldBe(paidTx.Id.Value);
    }
}
