using Application.Common.Options;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Application.Wallet.Features.Commands.InitiateWalletTopUp;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Interfaces;
using Microsoft.Extensions.Options;
using SharedKernel.Exceptions;

namespace Tests.Application.Wallet.Features.Commands.InitiateWalletTopUp;

public sealed class InitiateWalletTopUpHandlerTests
{
    private readonly IWalletTopUpRepository _topUpRepository = Substitute.For<IWalletTopUpRepository>();
    private readonly IPaymentGatewayFactory _gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IOptions<ApiBaseUrlOptions> _apiOptions = Options.Create(new ApiBaseUrlOptions { PublicBaseUrl = "https://api.example.com" });

    private readonly InitiateWalletTopUpHandler _sut;

    public InitiateWalletTopUpHandlerTests()
    {
        _gatewayFactory.GetGateway(Arg.Any<string>()).Returns(_gateway);
        _gateway.GatewayName.Returns("zarinpal");
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet:topup", true));

        _sut = new InitiateWalletTopUpHandler(
            _topUpRepository, _gatewayFactory, _currentUserService,
            _unitOfWork, _auditService, _distributedLock, _apiOptions);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsFailure()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(new InitiateWalletTopUpCommand(50_000m, "zarinpal"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenGatewaySucceeds_ReturnsSuccessWithPaymentUrl()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _gateway.InitiateAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Email?>(), Arg.Any<PhoneNumber?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("AUTH-1", "https://gateway/pay/AUTH-1", Guid.NewGuid()));

        var result = await _sut.Handle(new InitiateWalletTopUpCommand(50_000m, "zarinpal"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Authority.ShouldBe("AUTH-1");
        result.Value.PaymentUrl.ShouldBe("https://gateway/pay/AUTH-1");
        result.Value.Amount.ShouldBe(50_000m);
        result.Value.Gateway.ShouldBe("zarinpal");
        await _topUpRepository.Received(1).AddAsync(Arg.Any<WalletTopUp>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGatewayThrowsExternalServiceException_MarksFailedAndReturnsFailure()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _gateway.InitiateAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Email?>(), Arg.Any<PhoneNumber?>(), Arg.Any<CancellationToken>())
            .Returns<Task<PaymentInitiationResult>>(_ => throw new ExternalServiceException("Zarinpal", "gateway offline"));

        var result = await _sut.Handle(new InitiateWalletTopUpCommand(50_000m, "zarinpal"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _auditService.Received().LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGatewayNameEmpty_UsesZarinpalAsDefault()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _gateway.InitiateAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Email?>(), Arg.Any<PhoneNumber?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("A", "u", Guid.NewGuid()));

        var result = await _sut.Handle(new InitiateWalletTopUpCommand(50_000m, ""), CancellationToken.None);

        result.ShouldBeSuccess();
        _gatewayFactory.Received().GetGateway("zarinpal");
    }
}
