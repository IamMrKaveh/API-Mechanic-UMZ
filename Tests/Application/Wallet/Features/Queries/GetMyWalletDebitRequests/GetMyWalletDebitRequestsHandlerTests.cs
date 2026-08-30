using Application.Wallet.Features.Queries.GetMyWalletDebitRequests;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;

namespace Tests.Application.Wallet.Features.Queries.GetMyWalletDebitRequests;

public sealed class GetMyWalletDebitRequestsHandlerTests
{
    private readonly IWalletDebitRequestRepository _debitRequestRepository = Substitute.For<IWalletDebitRequestRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyWalletDebitRequestsHandler _sut;

    public GetMyWalletDebitRequestsHandlerTests()
    {
        _sut = new GetMyWalletDebitRequestsHandler(_debitRequestRepository, _currentUserService);
    }

    private (UserId ownerId, WalletDebitRequest request) BuildRequest(
        decimal amount = 100_000m,
        TimeSpan? expiry = null)
    {
        var ownerId = UserId.NewId();
        var (_, req) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId)
            .WithAmount(amount)
            .WithExpiry(expiry ?? TimeSpan.FromHours(72))
            .Build();
        return (ownerId, req);
    }

    private void SetCurrentUser(UserId userId) => _currentUserService.UserId.Returns(userId.Value);

    [Fact]
    public async Task Handle_WhenStatusIsNull_QueriesRepositoryWithNullStatus()
    {
        var (ownerId, req) = BuildRequest();
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(ownerId, null, Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetMyWalletDebitRequestsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(1);
        await _debitRequestRepository.Received(1).GetByOwnerAsync(
            Arg.Is<UserId>(u => u == ownerId),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStatusProvided_QueriesRepositoryWithThatStatus()
    {
        var (ownerId, req) = BuildRequest();
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(ownerId, WalletDebitRequestStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetMyWalletDebitRequestsQuery(WalletDebitRequestStatus.Pending);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _debitRequestRepository.Received(1).GetByOwnerAsync(
            Arg.Is<UserId>(u => u == ownerId),
            WalletDebitRequestStatus.Pending,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoRequests_ReturnsEmptyList()
    {
        var ownerId = UserId.NewId();
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest>());

        var query = new GetMyWalletDebitRequestsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPendingRequestExpiresWithinSixHours_MarksIsExpiringSoonTrue()
    {
        var (ownerId, req) = BuildRequest(expiry: TimeSpan.FromHours(3));
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetMyWalletDebitRequestsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var dto = result.Value.Single();
        dto.IsExpiringSoon.ShouldBeTrue();
        dto.Status.ShouldBe(WalletDebitRequestStatus.Pending.ToString());
    }

    [Fact]
    public async Task Handle_WhenPendingRequestExpiresFarInFuture_MarksIsExpiringSoonFalse()
    {
        var (ownerId, req) = BuildRequest(expiry: TimeSpan.FromDays(3));
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetMyWalletDebitRequestsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Single().IsExpiringSoon.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotPending_IsExpiringSoonIsAlwaysFalse()
    {
        var (ownerId, req) = BuildRequest(expiry: TimeSpan.FromHours(1));
        req.Reject(UserId.NewId(), "not needed");
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetMyWalletDebitRequestsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        var dto = result.Value.Single();
        dto.IsExpiringSoon.ShouldBeFalse();
        dto.Status.ShouldBe(WalletDebitRequestStatus.Rejected.ToString());
    }

    [Fact]
    public async Task Handle_WhenMappingRequest_CopiesAllFieldsIntoDto()
    {
        var ownerId = UserId.NewId();
        var (_, req) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId)
            .WithAmount(250_000m)
            .Build();
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { req });

        var query = new GetMyWalletDebitRequestsQuery(null);

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
        dto.CreatedAt.ShouldBe(req.CreatedAt);
        dto.ExpiresAt.ShouldBe(req.ExpiresAt);
        dto.RespondedAt.ShouldBe(req.RespondedAt);
        dto.RejectionReason.ShouldBe(req.RejectionReason);
        dto.RequestedBy.ShouldBe(req.RequestedBy.Value);
    }

    [Fact]
    public async Task Handle_WhenMultipleRequests_ReturnsAllInSameOrderAsRepository()
    {
        var ownerId = UserId.NewId();
        var (_, first) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithAmount(100m).Build();
        var (_, second) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithAmount(200m).Build();
        var (_, third) = new WalletDebitRequestBuilder().WithOwner(ownerId).WithAmount(300m).Build();
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest> { first, second, third });

        var query = new GetMyWalletDebitRequestsQuery(null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(3);
        result.Value[0].Amount.ShouldBe(100m);
        result.Value[1].Amount.ShouldBe(200m);
        result.Value[2].Amount.ShouldBe(300m);
    }

    [Fact]
    public async Task Handle_WhenCancellationTokenProvided_PropagatesTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var ownerId = UserId.NewId();
        SetCurrentUser(ownerId);
        _debitRequestRepository.GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<WalletDebitRequestStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<WalletDebitRequest>());

        var query = new GetMyWalletDebitRequestsQuery(null);

        await _sut.Handle(query, cts.Token);

        await _debitRequestRepository.Received(1).GetByOwnerAsync(
            Arg.Any<UserId>(), null, cts.Token);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsEmpty_ThrowsDomainException()
    {
        _currentUserService.UserId.Returns(Guid.Empty);

        var query = new GetMyWalletDebitRequestsQuery(null);

        var act = async () => await _sut.Handle(query, CancellationToken.None);

        await act.ShouldThrowAsync<DomainException>();
    }
}
