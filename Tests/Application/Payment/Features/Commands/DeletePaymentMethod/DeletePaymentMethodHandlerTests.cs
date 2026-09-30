using Application.Common.Interfaces;
using Application.Payment.Features.Commands.DeletePaymentMethod;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using PaymentMethods = Domain.Payment.Aggregates.PaymentMethod;

namespace Tests.Application.Payment.Features.Commands.DeletePaymentMethod;

public class DeletePaymentMethodHandlerTests : HandlerTestBase
{
    private readonly IPaymentMethodRepository _repository = Substitute.For<IPaymentMethodRepository>(); private readonly DeletePaymentMethodHandler _sut;

    public DeletePaymentMethodHandlerTests()
    {
        _sut = new DeletePaymentMethodHandler(_repository, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ReturnsNotFound()
    {
        _repository
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethods?)null);

        var result = await _sut.Handle(new DeletePaymentMethodCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _repository.DidNotReceive().Update(Arg.Any<PaymentMethods>());
    }

    [Fact]
    public async Task Handle_WhenAnonymousCallerAndMethodExists_SoftDeletesWithoutDeletedBy()
    {
        var method = new PaymentMethodBuilder().Build();
        CurrentUserService.UserId.Returns((Guid?)null);
        _repository
            .GetByIdAsync(Arg.Is<PaymentMethodId>(x => x == method.Id), Arg.Any<CancellationToken>())
            .Returns(method);

        var result = await _sut.Handle(new DeletePaymentMethodCommand(method.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        method.IsDeleted.ShouldBeTrue();
        method.DeletedBy.ShouldBeNull();
        _repository.Received(1).Update(method);
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedCallerAndMethodExists_SoftDeletesWithDeletedBy()
    {
        var method = new PaymentMethodBuilder().Build();
        var userId = Guid.NewGuid();
        CurrentUserService.UserId.Returns(userId);
        _repository
            .GetByIdAsync(Arg.Is<PaymentMethodId>(x => x == method.Id), Arg.Any<CancellationToken>())
            .Returns(method);

        var result = await _sut.Handle(new DeletePaymentMethodCommand(method.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        method.IsDeleted.ShouldBeTrue();
        method.DeletedBy.ShouldBe(userId);
        _repository.Received(1).Update(method);
    }
}
