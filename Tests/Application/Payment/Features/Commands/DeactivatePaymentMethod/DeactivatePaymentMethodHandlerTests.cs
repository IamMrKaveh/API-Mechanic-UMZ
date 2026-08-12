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
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>(); private readonly DeactivatePaymentMethodHandler _sut;

    public DeactivatePaymentMethodHandlerTests()
    {
        _sut = new DeactivatePaymentMethodHandler(_repository);
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
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodExists_DeactivatesAndUpdatesRepository()
    {
        var method = new PaymentMethodBuilder().Build();
        method.IsActive.ShouldBeTrue();

        _repository
            .GetByIdAsync(Arg.Is<PaymentMethodId>(x => x.Value == method.Id.Value), Arg.Any<CancellationToken>())
            .Returns(method);

        var result = await _sut.Handle(new DeactivatePaymentMethodCommand(method.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        method.IsActive.ShouldBeFalse();
        _repository.Received(1).Update(method);
    }
}
