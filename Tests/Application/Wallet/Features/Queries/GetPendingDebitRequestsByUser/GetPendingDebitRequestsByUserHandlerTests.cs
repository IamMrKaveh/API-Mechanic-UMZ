using Application.Wallet.Features.Queries.GetPendingDebitRequestsByUser;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;

namespace Tests.Application.Wallet.Features.Queries.GetPendingDebitRequestsByUser;

public sealed class GetPendingDebitRequestsByUserHandlerTests
{
    private readonly IWalletDebitRequestRepository _debitRequestRepository = Substitute.For<IWalletDebitRequestRepository>();
    private readonly GetPendingDebitRequestsByUserHandler _sut;

    public GetPendingDebitRequestsByUserHandlerTests()
    {
        _sut = new GetPendingDebitRequestsByUserHandler(_debitRequestRepository);
    }

    [Fact]
    public async Task Handle_WhenPendingRequestsExist_ReturnsSuccessWithMappedDtos()
    {
        var ownerId = UserId.NewId();
        var (_, first) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithAmount(100_000m).Build();
        var (_, second) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithAmount(200_000m).Build();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { first, second });

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Amount.ShouldBe(100_000m);
        result.Value[1].Amount.ShouldBe(200_000m);
    }

    [Fact]
    public async Task Handle_WhenNoPendingRequests_ReturnsEmptyList()
    {
        var ownerId = UserId.NewId();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest>());

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCalled_PassesQueryUserIdToRepositoryAsUserId()
    {
        var ownerId = UserId.NewId();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest>());

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        await _sut.Handle(query, CancellationToken.None);

        await _debitRequestRepository.Received(1).GetPendingByOwnerAsync(
            Arg.Is<UserId>(u => u == ownerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPendingRequestExpiresWithinSixHours_MarksIsExpiringSoonTrue()
    {
        var ownerId = UserId.NewId();
        var (_, req) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId)
            .WithExpiry(TimeSpan.FromHours(3))
            .Build();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Single().IsExpiringSoon.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenPendingRequestExpiresFarInFuture_MarksIsExpiringSoonFalse()
    {
        var ownerId = UserId.NewId();
        var (_, req) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId)
            .WithExpiry(TimeSpan.FromDays(2))
            .Build();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Single().IsExpiringSoon.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenMappingDto_CopiesAllFieldsFromEntity()
    {
        var ownerId = UserId.NewId();
        var (_, req) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithAmount(300_000m).Build();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var dto = result.Value.Single();
        dto.Id.ShouldBe(req.Id.Value);
        dto.WalletId.ShouldBe(req.WalletId.Value);
        dto.OwnerId.ShouldBe(req.OwnerId.Value);
        dto.Amount.ShouldBe(req.Amount.Amount);
        dto.Currency.ShouldBe(req.Amount.Currency);
        dto.Reason.ShouldBe(req.Reason);
        dto.Description.ShouldBe(req.Description);
        dto.Status.ShouldBe(req.Status.ToString());
        dto.RequestedBy.ShouldBe(req.RequestedBy.Value);
        dto.CreatedAt.ShouldBe(req.CreatedAt);
        dto.ExpiresAt.ShouldBe(req.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var ownerId = UserId.NewId();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest>());

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        await _sut.Handle(query, cts.Token);

        await _debitRequestRepository.Received(1).GetPendingByOwnerAsync(
            Arg.Any<UserId>(), cts.Token);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ThrowsDomainException()
    {
        var query = new GetPendingDebitRequestsByUserQuery(Guid.Empty);

        var act = async () => await _sut.Handle(query, CancellationToken.None);

        await act.ShouldThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenAllPendingReturned_AllHavePendingStatusInDto()
    {
        var ownerId = UserId.NewId();
        var (_, a) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        var (_, b) = new WalletDebitRequestBuilder().WithOwner(ownerId).Build();
        _debitRequestRepository.GetPendingByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { a, b });

        var query = new GetPendingDebitRequestsByUserQuery(ownerId.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldAllBe(d => d.Status == WalletDebitRequestStatus.Pending.ToString());
    }
}
