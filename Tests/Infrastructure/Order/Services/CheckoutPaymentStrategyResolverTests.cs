using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutPaymentStrategyResolverTests
{
    private readonly IPaymentMethodRepository _paymentMethodRepository = Substitute.For<IPaymentMethodRepository>();
    private readonly ICheckoutPaymentStrategy _zarinpal;
    private readonly ICheckoutPaymentStrategy _cod;
    private readonly ICheckoutPaymentStrategy _wallet;

    public CheckoutPaymentStrategyResolverTests()
    {
        _zarinpal = Substitute.For<ICheckoutPaymentStrategy>();
        _zarinpal.Code.Returns(PaymentMethodCode.Zarinpal);
        _cod = Substitute.For<ICheckoutPaymentStrategy>();
        _cod.Code.Returns(PaymentMethodCode.CashOnDelivery);
        _wallet = Substitute.For<ICheckoutPaymentStrategy>();
        _wallet.Code.Returns(PaymentMethodCode.Wallet);
    }

    private CheckoutPaymentStrategyResolver BuildSut() =>
        new([_zarinpal, _cod, _wallet], _paymentMethodRepository);

    private static global::Domain.Payment.Aggregates.PaymentMethod ActiveMethod(string code)
    {
        var method = new PaymentMethodBuilder().WithCode(code).Build();
        return method;
    }

    [Fact]
    public async Task ResolveAsync_WhenMethodIdMatchesActiveMethod_ReturnsItsStrategy()
    {
        var method = ActiveMethod("zarinpal");
        _paymentMethodRepository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        var sut = BuildSut();

        var result = await sut.ResolveAsync(method.Id.Value, null, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(_zarinpal);
    }

    [Fact]
    public async Task ResolveAsync_WhenMethodDoesNotExist_ReturnsNotFound()
    {
        _paymentMethodRepository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Payment.Aggregates.PaymentMethod?)null);
        var sut = BuildSut();

        var result = await sut.ResolveAsync(Guid.NewGuid(), null, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_WhenMethodIsInactive_ReturnsFailure()
    {
        var method = new PaymentMethodBuilder().WithCode("zarinpal").Build();
        method.Deactivate();
        _paymentMethodRepository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        var sut = BuildSut();

        var result = await sut.ResolveAsync(method.Id.Value, null, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ResolveAsync_WhenMethodIsDeleted_ReturnsFailure()
    {
        var method = new PaymentMethodBuilder().WithCode("wallet").Build();
        method.RequestDeletion();
        _paymentMethodRepository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        var sut = BuildSut();

        var result = await sut.ResolveAsync(method.Id.Value, null, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Theory]
    [InlineData("cash-on-delivery", "cash-on-delivery")]
    [InlineData("CashOnDelivery", "cash-on-delivery")]
    [InlineData("CASH_ON_DELIVERY", "cash-on-delivery")]
    [InlineData("wallet", "wallet")]
    [InlineData("Wallet", "wallet")]
    [InlineData("zarinpal-sandbox", "zarinpal")]
    [InlineData("ZarinPalSandbox", "zarinpal")]
    [InlineData("zarinpalsandbox", "zarinpal")]
    public async Task ResolveAsync_WhenGatewayMatchesAlias_ResolvesCanonicalStrategy(
        string gateway, string expectedCode)
    {
        var sut = BuildSut();

        var result = await sut.ResolveAsync(null, gateway, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task ResolveAsync_WhenNeitherIdNorGateway_ReturnsFailure()
    {
        var sut = BuildSut();

        var result = await sut.ResolveAsync(null, null, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ResolveAsync_WhenGatewayIsWhitespace_ReturnsFailure()
    {
        var sut = BuildSut();

        var result = await sut.ResolveAsync(null, "   ", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ResolveAsync_WhenCodeIsUnsupported_ReturnsFailure()
    {
        var sut = BuildSut();

        var result = await sut.ResolveAsync(null, "bitcoin", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain("bitcoin");
    }

    [Fact]
    public async Task ResolveAsync_WhenMethodIdIsEmptyGuid_FallsBackToGateway()
    {
        var sut = BuildSut();

        var result = await sut.ResolveAsync(Guid.Empty, "wallet", CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(_wallet);
        await _paymentMethodRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task ResolveAsync_PrefersMethodIdOverGateway()
    {
        var method = ActiveMethod("wallet");
        _paymentMethodRepository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        var sut = BuildSut();

        var result = await sut.ResolveAsync(method.Id.Value, "zarinpal", CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(_wallet);
    }
}
