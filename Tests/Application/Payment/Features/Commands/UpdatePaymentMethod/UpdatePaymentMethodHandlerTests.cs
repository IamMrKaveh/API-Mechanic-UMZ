using Application.Payment.Features.Commands.UpdatePaymentMethod;
using Application.Payment.Features.Shared;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using PaymentMethods = Domain.Payment.Aggregates.PaymentMethod;

namespace Tests.Application.Payment.Features.Commands.UpdatePaymentMethod;

public class UpdatePaymentMethodHandlerTests
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly UpdatePaymentMethodHandler _sut;

    public UpdatePaymentMethodHandlerTests()
    {
        _sut = new UpdatePaymentMethodHandler(_repository, _mapper);
    }

    private static UpdatePaymentMethodCommand CommandFor(Guid id) =>
        new(id, "Zarinpal Updated", "توضیح", null, 0m, 0m, 5);

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethods?)null);

        var result = await _sut.Handle(CommandFor(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _repository.DidNotReceive().Update(Arg.Any<PaymentMethods>());
    }

    [Fact]
    public async Task Handle_WhenNameConflict_ReturnsConflict()
    {
        var method = new PaymentMethodBuilder().Build();

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        _repository
            .ExistsByNameAsync(Arg.Any<PaymentMethodName>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(CommandFor(method.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        _repository.DidNotReceive().Update(Arg.Any<PaymentMethods>());
    }

    [Fact]
    public async Task Handle_WhenValidAndUnique_UpdatesAndReturnsSuccess()
    {
        var method = new PaymentMethodBuilder().Build();

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        _repository
            .ExistsByNameAsync(Arg.Any<PaymentMethodName>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var expected = new PaymentMethodDto { Id = method.Id.Value, Name = "Zarinpal Updated" };
        _mapper.Map<PaymentMethodDto>(method).Returns(expected);

        var result = await _sut.Handle(CommandFor(method.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        _repository.Received(1).Update(method);
    }

    [Fact]
    public async Task Handle_WhenDomainThrows_ReturnsValidation()
    {
        var method = new PaymentMethodBuilder().Build();

        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns(method);
        _repository
            .ExistsByNameAsync(Arg.Any<PaymentMethodName>(), Arg.Any<PaymentMethodId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new UpdatePaymentMethodCommand(method.Id.Value, "", null, null, 0m, 0m, 0);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
        _repository.DidNotReceive().Update(Arg.Any<PaymentMethods>());
    }
}
