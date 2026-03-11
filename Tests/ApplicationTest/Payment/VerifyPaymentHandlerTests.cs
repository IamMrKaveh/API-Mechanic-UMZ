namespace Tests.ApplicationTest.Payment;

public class VerifyPaymentHandlerTests
{
    private readonly IPaymentService _paymentService;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;
    private readonly ILogger<VerifyPaymentHandler> _logger;
    private readonly VerifyPaymentHandler _handler;

    public VerifyPaymentHandlerTests()
    {
        _paymentService = Substitute.For<IPaymentService>();
        _frontendUrls = Options.Create(new FrontendUrlsDto { BaseUrl = "https://example.com" });
        _logger = Substitute.For<ILogger<VerifyPaymentHandler>>();
        _handler = new VerifyPaymentHandler(_paymentService, _frontendUrls, _logger);
    }

    [Fact]
    public async Task Handle_WhenVerificationSucceeds_ShouldReturnSuccessWithRefId()
    {
        var serviceResult = ServiceResult<(bool IsVerified, long? RefId, string? CardPan, string? Message)>
            .Success((true, 987654321L, "6037-****-1234", null));
        _paymentService.VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(serviceResult);

        var result = await _handler.Handle(new VerifyPaymentCommand("AUTH123", "StatSuccess"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsSuccess.Should().BeTrue();
        result.Value.RefId.Should().Be(987654321L);
        result.Value.RedirectUrl.Should().Contain("success");
    }

    [Fact]
    public async Task Handle_WhenVerificationFails_ShouldReturnFailureRedirectUrl()
    {
        var serviceResult = ServiceResult<(bool IsVerified, long? RefId, string? CardPan, string? Message)>
            .Success((false, null, null, "پرداخت انجام نشد"));
        _paymentService.VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(serviceResult);

        var result = await _handler.Handle(new VerifyPaymentCommand("AUTH_FAIL", "StatFail"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsSuccess.Should().BeFalse();
        result.Value.RedirectUrl.Should().Contain("failure");
    }

    [Fact]
    public async Task Handle_WhenServiceCallFails_ShouldReturnSuccessWithFailureUrl()
    {
        var serviceResult = ServiceResult<(bool IsVerified, long? RefId, string? CardPan, string? Message)>
            .Failure("خطای ارتباط با سرور", 503);
        _paymentService.VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(serviceResult);

        var result = await _handler.Handle(new VerifyPaymentCommand("AUTH123", "StatSuccess"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ShouldReturnFailure()
    {
        _paymentService.VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new DomainException("تراکنش منقضی شده است."));

        var result = await _handler.Handle(new VerifyPaymentCommand("AUTH123", "StatFail"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("تراکنش منقضی");
    }

    [Fact]
    public async Task Handle_SuccessRedirectUrl_ShouldContainBaseUrl()
    {
        var serviceResult = ServiceResult<(bool IsVerified, long? RefId, string? CardPan, string? Message)>
            .Success((true, 111L, null, null));
        _paymentService.VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(serviceResult);

        var result = await _handler.Handle(new VerifyPaymentCommand("AUTH123", "StatSuccess"), CancellationToken.None);

        result.Value!.RedirectUrl.Should().StartWith("https://example.com");
    }
}