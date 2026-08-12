using Application.Common.Interfaces;
using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetPaymentByAuthority;
using Application.Payment.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Queries.GetPaymentByAuthority;

public class GetPaymentByAuthorityHandlerTests
{
    private readonly IPaymentQueryService _paymentQueryService = Substitute.For<IPaymentQueryService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetPaymentByAuthorityHandler _sut;

    public GetPaymentByAuthorityHandlerTests()
    {
        _sut = new GetPaymentByAuthorityHandler(_paymentQueryService, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserIsAnonymous_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetPaymentByAuthorityQuery("A123"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _paymentQueryService.DidNotReceiveWithAnyArgs().GetByAuthorityAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns((PaymentTransactionDto?)null);

        var result = await _sut.Handle(new GetPaymentByAuthorityQuery("A123"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAccessesOtherUsersTransaction_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);

        var dto = new PaymentTransactionDto { Id = Guid.NewGuid(), UserId = ownerId };
        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetPaymentByAuthorityQuery("A123"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenOwnerAccessesOwnTransaction_ReturnsSuccess()
    {
        var callerId = Guid.NewGuid();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);

        var dto = new PaymentTransactionDto { Id = Guid.NewGuid(), UserId = callerId };
        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetPaymentByAuthorityQuery("A123"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_WhenAdminAccessesAnyTransaction_ReturnsSuccess()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsAdmin.Returns(true);

        var dto = new PaymentTransactionDto { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetPaymentByAuthorityQuery("A123"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }
}
