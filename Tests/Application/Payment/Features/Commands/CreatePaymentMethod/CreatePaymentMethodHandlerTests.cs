using Application.Cache.Contracts;
using Application.Payment.Features.Commands.CreatePaymentMethod;
using Application.Payment.Features.Shared;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using PaymentMethods = Domain.Payment.Aggregates.PaymentMethod;

namespace Tests.Application.Payment.Features.Commands.CreatePaymentMethod;

public class CreatePaymentMethodHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreatePaymentMethodHandler _sut;

    public CreatePaymentMethodHandlerTests()
    {
        _sut = new CreatePaymentMethodHandler(_repository, _mapper, _cacheService);
    }

    private static CreatePaymentMethodCommand ValidCommand() =>
        new("Zarinpal", "zarinpal", "درگاه", null, 0m, 0m, 1);

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ReturnsConflict()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<PaymentMethodName>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PaymentMethods>(), Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ReturnsConflict()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<PaymentMethodName>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsByCodeAsync(Arg.Any<PaymentMethodCode>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PaymentMethods>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidAndUnique_AddsPaymentMethodAndReturnsSuccess()
    {
        _repository
            .ExistsByNameAsync(Arg.Any<PaymentMethodName>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository
            .ExistsByCodeAsync(Arg.Any<PaymentMethodCode>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var expected = new PaymentMethodDto { Name = "Zarinpal", Code = "zarinpal" };
        _mapper.Map<PaymentMethodDto>(Arg.Any<PaymentMethods>()).Returns(expected);

        var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        await _repository.Received(1).AddAsync(Arg.Any<PaymentMethods>(), Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveByPrefixAsync("payment-methods:", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WhenNameIsInvalidDomainValue_ReturnsValidation(string invalidName)
    {
        var cmd = new CreatePaymentMethodCommand(invalidName, "zarinpal", null, null, 0m, 0m, 0);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PaymentMethods>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCodeContainsInvalidCharacters_ReturnsValidation()
    {
        var cmd = new CreatePaymentMethodCommand("Zarinpal", "Zarin Pal!", null, null, 0m, 0m, 0);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PaymentMethods>(), Arg.Any<CancellationToken>());
    }
}
