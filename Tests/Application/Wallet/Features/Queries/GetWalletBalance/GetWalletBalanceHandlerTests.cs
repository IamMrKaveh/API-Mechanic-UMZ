using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Queries.GetWalletBalance;

public class GetWalletBalanceHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetWalletBalanceHandler _sut;

    public GetWalletBalanceHandlerTests()
    {
        _sut = new GetWalletBalanceHandler(_walletRepository, _unitOfWork, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenWalletExistsForCurrentUser_ReturnsSuccessWithBalancesFromExistingWallet()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var wallet = new WalletBuilder().WithOwnerId(UserId.From(currentUserId)).Build();

        _walletRepository
            .GetByUserIdAsync(Arg.Is<UserId>(x => x!.Value == currentUserId), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(new GetWalletBalanceQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(wallet.Id.Value);
        result.Value.UserId.ShouldBe(currentUserId);
        result.Value.CurrentBalance.ShouldBe(wallet.Balance.Amount);
        result.Value.ReservedBalance.ShouldBe(wallet.ReservedBalance.Amount);
        result.Value.AvailableBalance.ShouldBe(wallet.AvailableBalance.Amount);

        await _walletRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenExplicitUserIdProvided_UsesRequestUserIdInsteadOfCurrentUser()
    {
        var currentUserId = Guid.NewGuid();
        var requestedUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var wallet = new WalletBuilder().WithOwnerId(UserId.From(requestedUserId)).Build();

        _walletRepository
            .GetByUserIdAsync(Arg.Is<UserId>(x => x!.Value == requestedUserId), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(new GetWalletBalanceQuery(requestedUserId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.UserId.ShouldBe(requestedUserId);

        await _walletRepository.Received(1)
            .GetByUserIdAsync(Arg.Is<UserId>(x => x!.Value == requestedUserId), Arg.Any<CancellationToken>());
        await _walletRepository.DidNotReceive()
            .GetByUserIdAsync(Arg.Is<UserId>(x => x!.Value == currentUserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWalletDoesNotExist_CreatesNewWalletAndPersistsIt()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        Wallets? addedWallet = null;
        _walletRepository
            .AddAsync(Arg.Do<Wallets>(w => addedWallet = w), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.Handle(new GetWalletBalanceQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        addedWallet.ShouldNotBeNull();
        addedWallet!.OwnerId.Value.ShouldBe(currentUserId);
        addedWallet.Balance.Amount.ShouldBe(0m);

        result.Value.Id.ShouldBe(addedWallet.Id.Value);
        result.Value.UserId.ShouldBe(currentUserId);
        result.Value.CurrentBalance.ShouldBe(0m);
        result.Value.ReservedBalance.ShouldBe(0m);
        result.Value.AvailableBalance.ShouldBe(0m);

        await _walletRepository.Received(1).AddAsync(Arg.Any<Wallets>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestUserIdIsEmptyGuid_FallsBackToCurrentUserId()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var wallet = new WalletBuilder().WithOwnerId(UserId.From(currentUserId)).Build();

        _walletRepository
            .GetByUserIdAsync(Arg.Is<UserId>(x => x!.Value == currentUserId), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var query = new GetWalletBalanceQuery(userId: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.UserId.ShouldBe(currentUserId);
    }

    [Fact]
    public async Task Handle_MapsAllBalancePropertiesFromWalletAggregateIntoDto()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentUserId);

        var wallet = new WalletBuilder().WithOwnerId(UserId.From(currentUserId)).Build();

        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _sut.Handle(new GetWalletBalanceQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        var dto = result.Value;
        dto.ShouldBeOfType<WalletDto>();
        dto.CurrentBalance.ShouldBe(wallet.Balance.Amount);
        dto.ReservedBalance.ShouldBe(wallet.ReservedBalance.Amount);
        dto.AvailableBalance.ShouldBe(wallet.Balance.Amount - wallet.ReservedBalance.Amount);
    }
}
