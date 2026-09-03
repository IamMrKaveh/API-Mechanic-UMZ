using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.Aggregates;
using Domain.Order.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Exceptions;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.Services;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.Extensions.Options;
using SharedContracts.FeatureManagement;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Payment.Services;

public class PaymentServiceTests
{
    private readonly IPaymentTransactionRepository _paymentRepository = Substitute.For<IPaymentTransactionRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IPaymentGatewayFactory _gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IPaymentCallbackNonceService _nonceService = Substitute.For<IPaymentCallbackNonceService>();
    private readonly IFeatureManager _featureManager = Substitute.For<IFeatureManager>();
    private readonly PaymentService _sut;

    private static readonly DateTime FixedNow = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    public PaymentServiceTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _gatewayFactory.GetGateway(Arg.Any<string>()).Returns(_gateway);
        _gateway.GatewayName.Returns("Zarinpal");
        _currentUserService.FrontendBaseUrl.Returns("https://shop.example.com");
        _nonceService.IssueAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("nonce-123");
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(false);
        _sut = new PaymentService(
            _paymentRepository,
            _orderRepository,
            _gatewayFactory,
            _unitOfWork,
            _dateTimeProvider,
            _auditService,
            Options.Create(new ZarinPalOptions
            {
                StartPayBaseUrl = "https://pay.example/StartPay/",
                SandboxStartPayBaseUrl = "https://sandbox.example/StartPay/",
                UseSandbox = false
            }),
            _currentUserService,
            _nonceService,
            _featureManager);
    }

    private static global::Domain.Order.Aggregates.Order NewOrder(UserId? userId = null)
    {
        var order = new OrderBuilder()
            .WithUserId(userId ?? UserId.NewId())
            .Build();
        order.ClearDomainEvents();
        return order;
    }

    private static PaymentTransaction NewTransaction(OrderId orderId, UserId userId) =>
        new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithAuthority("A" + new string('1', 24))
            .WithAmount(150_000m)
            .Build();

    [Fact]
    public async Task InitiatePaymentAsync_WhenOrderDoesNotExist_ThrowsOrderNotFoundException()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Order.Aggregates.Order?)null);

        await Should.ThrowAsync<OrderNotFoundException>(() => _sut.InitiatePaymentAsync(
            OrderId.NewId(), Money.FromDecimal(150_000m),
            IpAddress.Create("127.0.0.1"), UserId.NewId(), null, CancellationToken.None));
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenActivePaymentExists_ReturnsExistingWithoutCreating()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var existing = NewTransaction(order.Id, userId);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetActiveByOrderIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.InitiatePaymentAsync(
            order.Id, order.FinalAmount, IpAddress.Create("127.0.0.1"), userId, null, CancellationToken.None);

        result.Authority.ShouldBe(existing.Authority.Value);
        result.PaymentUrl.ShouldContain(existing.Authority.Value);
        await _paymentRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenNoActivePayment_CreatesTransactionAndMovesOrderToPending()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetActiveByOrderIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);
        _gateway.InitiateAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Email?>(), Arg.Any<PhoneNumber?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("A" + new string('2', 24), "https://pay.example/1", Guid.NewGuid()));

        PaymentTransaction? captured = null;
        await _paymentRepository.AddAsync(Arg.Do<PaymentTransaction>(t => captured = t), Arg.Any<CancellationToken>());

        var result = await _sut.InitiatePaymentAsync(
            order.Id, order.FinalAmount, IpAddress.Create("127.0.0.1"), userId, null, CancellationToken.None);

        result.Authority.ShouldNotBeNullOrWhiteSpace();
        captured.ShouldNotBeNull();
        captured!.OrderId.ShouldBe(order.Id);
        order.Status.ShouldBe(OrderStatusValue.Pending);
        _orderRepository.Received(1).Update(order);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenGatewayFactoryFails_ThrowsExternalServiceException()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetActiveByOrderIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);
        _gatewayFactory.GetGateway(Arg.Any<string>()).Throws(new InvalidOperationException("no gateway"));

        await Should.ThrowAsync<ExternalServiceException>(() => _sut.InitiatePaymentAsync(
            order.Id, order.FinalAmount, IpAddress.Create("127.0.0.1"), userId, null, CancellationToken.None));
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenGatewayInitiationFails_LogsErrorAndRethrows()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetActiveByOrderIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);
        _gateway.InitiateAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Email?>(), Arg.Any<PhoneNumber?>(), Arg.Any<CancellationToken>())
            .Throws(new ExternalServiceException("Zarinpal", "gateway down"));

        await Should.ThrowAsync<ExternalServiceException>(() => _sut.InitiatePaymentAsync(
            order.Id, order.FinalAmount, IpAddress.Create("127.0.0.1"), userId, null, CancellationToken.None));

        await _auditService.Received(1).LogErrorAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenTransactionDoesNotExist_ThrowsNotFound()
    {
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);

        await Should.ThrowAsync<PaymentTransactionNotFoundException>(() =>
            _sut.VerifyPaymentAsync("A" + new string('3', 24), CancellationToken.None));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenAlreadySuccessful_MarksOrderPaidAndReturnsVerified()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var transaction = NewTransaction(order.Id, userId);
        transaction.MarkAsSuccess(123456L, FixedNow);
        transaction.ClearDomainEvents();
        order.ClearDomainEvents();
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.VerifyPaymentAsync(transaction.Authority.Value, CancellationToken.None);

        result.IsVerified.ShouldBeTrue();
        result.RefId.ShouldBe(123456L);
        order.IsPaid.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenNotVerifiable_ThrowsPaymentNotVerifiableException()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(userId)
            .WithNow(FixedNow.AddHours(-2))
            .Build();
        transaction.MarkAsFailed(FixedNow.AddHours(-1), "user cancelled");
        transaction.ClearDomainEvents();
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        await Should.ThrowAsync<PaymentNotVerifiableException>(() =>
            _sut.VerifyPaymentAsync(transaction.Authority.Value, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenGatewayReportsInvalidRefId_ThrowsExternalServiceException()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var transaction = NewTransaction(order.Id, userId);
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _gateway.GatewayName.Returns(transaction.Gateway.Value);
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(Guid.NewGuid(), true, 0L, null, 0m));

        await Should.ThrowAsync<ExternalServiceException>(() =>
            _sut.VerifyPaymentAsync(transaction.Authority.Value, CancellationToken.None));

        await _auditService.Received(1).LogWarningAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenSuccessful_MarksTransactionAndOrder()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var transaction = NewTransaction(order.Id, userId);
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _gateway.GatewayName.Returns(transaction.Gateway.Value);
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(Guid.NewGuid(), true, 987654L, "6037-****", 1500m));

        var result = await _sut.VerifyPaymentAsync(transaction.Authority.Value, CancellationToken.None);

        result.IsVerified.ShouldBeTrue();
        result.RefId.ShouldBe(987654L);
        result.Fee.ShouldBe(1500m);
        transaction.IsSuccessful().ShouldBeTrue();
        order.IsPaid.ShouldBeTrue();
        _paymentRepository.Received(1).Update(transaction);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenTransactionDoesNotExist_ThrowsNotFound()
    {
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);

        await Should.ThrowAsync<PaymentTransactionNotFoundException>(() =>
            _sut.ProcessWebhookAsync("A" + new string('4', 24), "OK", null, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenNonceIsInvalid_ThrowsExternalServiceException()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var transaction = NewTransaction(order.Id, userId);
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        _featureManager.IsEnabledAsync(FeatureFlags.PaymentCallbackSignatureRequired).Returns(true);
        _nonceService.ValidateAndConsumeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Should.ThrowAsync<ExternalServiceException>(() =>
            _sut.ProcessWebhookAsync(transaction.Authority.Value, "OK", "bad-nonce", CancellationToken.None));

        await _auditService.Received(1).LogSecurityEventAsync(
            "PaymentWebhookInvalidNonce", Arg.Any<string>(), Arg.Any<IpAddress>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenCancelledByUser_MarksTransactionAsFailed()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var transaction = NewTransaction(order.Id, userId);
        _paymentRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        await _sut.ProcessWebhookAsync(transaction.Authority.Value, "NOK", null, CancellationToken.None);

        transaction.IsPending().ShouldBeFalse();
        _paymentRepository.Received(1).Update(transaction);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
