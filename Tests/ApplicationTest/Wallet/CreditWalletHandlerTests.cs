using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;

namespace Tests.ApplicationTest.Wallet;

public class CreditWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreditWalletHandler> _logger;
    private readonly CreditWalletHandler _handler;

    public CreditWalletHandlerTests()
    {
        _walletRepository = Substitute.For<IWalletRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<CreditWalletHandler>>();
        _handler = new CreditWalletHandler(_walletRepository, _unitOfWork, _logger);
    }

    private static CreditWalletCommand BuildCommand(decimal amount = 500_000m)
        => new CreditWalletCommand(
            UserId: 1,
            Amount: amount,
            TransactionType: WalletTransactionType.ManualCredit,
            ReferenceType: WalletReferenceType.Manual,
            ReferenceId: 1,
            IdempotencyKey: Guid.NewGuid().ToString());

    [Fact]
    public async Task Handle_WithExistingWallet_ShouldCreditAndReturnSuccess()
    {
        var wallet = new WalletBuilder().Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWalletNotExists_ShouldCreateNewWalletAndCredit()
    {
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Domain.Wallet.Aggregates.Wallet?)null);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _walletRepository.Received(1).AddAsync(Arg.Any<Domain.Wallet.Aggregates.Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnSuccessWithoutCrediting()
    {
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _walletRepository.DidNotReceive().GetByUserIdForUpdateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        var wallet = new WalletBuilder().Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(BuildCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingWallet_ShouldIncreaseBalance()
    {
        var wallet = new WalletBuilder().Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(BuildCommand(amount: 300_000m), CancellationToken.None);

        wallet.CurrentBalance.Should().Be(300_000m);
    }
}