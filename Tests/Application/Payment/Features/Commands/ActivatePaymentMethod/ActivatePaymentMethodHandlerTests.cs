using Application.Payment.Features.Commands.ActivatePaymentMethod;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using PaymentMethods = Domain.Payment.Aggregates.PaymentMethod;

namespace Tests.Application.Payment.Features.Commands.ActivatePaymentMethod;

public class ActivatePaymentMethodHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>(); private readonly ActivatePaymentMethodHandler _sut;

    public ActivatePaymentMethodHandlerTests()
    {
        _sut = new ActivatePaymentMethodHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethods?)null);

        var result = await _sut.Handle(new ActivatePaymentMethodCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _repository.DidNotReceive().Update(Arg.Any<PaymentMethods>());
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodExists_ActivatesAndUpdatesRepository()
    {
        var method = new PaymentMethodBuilder().Build();
        method.Deactivate();
        method.IsActive.ShouldBeFalse();

        _repository
            .GetByIdAsync(Arg.Is<PaymentMethodId>(x => x == method.Id), Arg.Any<CancellationToken>())
            .Returns(method);

        var result = await _sut.Handle(new ActivatePaymentMethodCommand(method.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        method.IsActive.ShouldBeTrue();
        _repository.Received(1).Update(method);
    }
}
