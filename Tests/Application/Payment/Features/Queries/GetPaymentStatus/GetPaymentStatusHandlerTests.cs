using Application.Common.Interfaces;
using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetPaymentStatus;
using Application.Payment.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Queries.GetPaymentStatus;

public class GetPaymentStatusHandlerTests
{
    private readonly IPaymentQueryService _paymentQueryService = Substitute.For<IPaymentQueryService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetPaymentStatusHandler _sut;

    public GetPaymentStatusHandlerTests()
    {
        _sut = new GetPaymentStatusHandler(_paymentQueryService, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserIsAnonymous_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetPaymentStatusQuery("A123"), CancellationToken.None);

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

        var result = await _sut.Handle(new GetPaymentStatusQuery("A123"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAccessesOtherUsersTransaction_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);

        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(new PaymentTransactionDto { UserId = ownerId });

        var result = await _sut.Handle(new GetPaymentStatusQuery("A123"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _paymentQueryService.DidNotReceiveWithAnyArgs().GetStatusByAuthorityAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenStatusDtoNotFound_ReturnsNotFound()
    {
        var callerId = Guid.NewGuid();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);

        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(new PaymentTransactionDto { UserId = callerId });

        _paymentQueryService
            .GetStatusByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns((PaymentStatusDto?)null);

        var result = await _sut.Handle(new GetPaymentStatusQuery("A123"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenOwnerRequestsOwnTransaction_ReturnsSuccess()
    {
        var callerId = Guid.NewGuid();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);

        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(new PaymentTransactionDto { UserId = callerId });

        var status = new PaymentStatusDto { Authority = "A123", Status = "Success", IsSuccess = true, Amount = 100m };
        _paymentQueryService
            .GetStatusByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(status);

        var result = await _sut.Handle(new GetPaymentStatusQuery("A123"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(status);
    }

    [Fact]
    public async Task Handle_WhenAdminRequestsAnyTransaction_ReturnsSuccess()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsAdmin.Returns(true);

        _paymentQueryService
            .GetByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(new PaymentTransactionDto { UserId = Guid.NewGuid() });

        var status = new PaymentStatusDto { Authority = "A123", Status = "Success", IsSuccess = true };
        _paymentQueryService
            .GetStatusByAuthorityAsync("A123", Arg.Any<CancellationToken>())
            .Returns(status);

        var result = await _sut.Handle(new GetPaymentStatusQuery("A123"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(status);
    }
}
