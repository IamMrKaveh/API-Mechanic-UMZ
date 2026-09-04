using Application.Cache.Contracts;
using Application.Payment.Features.Commands.DeactivatePaymentMethod;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using PaymentMethods = Domain.Payment.Aggregates.PaymentMethod;

namespace Tests.Application.Payment.Features.Commands.DeactivatePaymentMethod;

public class DeactivatePaymentMethodHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly DeactivatePaymentMethodHandler _sut;

    public DeactivatePaymentMethodHandlerTests()
    {
        _sut = new DeactivatePaymentMethodHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethods?)null);

        var result = await _sut.Handle(new DeactivatePaymentMethodCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _repository.DidNotReceive().Update(Arg.Any<PaymentMethods>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodExists_DeactivatesAndUpdatesRepository()
    {
        var method = new PaymentMethodBuilder().Build();
        method.IsActive.ShouldBeTrue();

        _repository
            .GetByIdAsync(Arg.Is<PaymentMethodId>(x => x == method.Id), Arg.Any<CancellationToken>())
            .Returns(method);

        var result = await _sut.Handle(new DeactivatePaymentMethodCommand(method.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        method.IsActive.ShouldBeFalse();
        _repository.Received(1).Update(method);
        await _cacheService.Received(1).RemoveByPrefixAsync("payment-methods:", Arg.Any<CancellationToken>());
    }
}
