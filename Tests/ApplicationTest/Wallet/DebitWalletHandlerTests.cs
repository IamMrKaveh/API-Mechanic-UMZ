using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;

namespace Tests.ApplicationTest.Wallet;

public class DebitWalletHandlerTests
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DebitWalletHandler> _logger;
    private readonly DebitWalletHandler _handler;

    public DebitWalletHandlerTests()
    {
        _walletRepository = Substitute.For<IWalletRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<DebitWalletHandler>>();
        _handler = new DebitWalletHandler(_walletRepository, _unitOfWork, _logger);
    }

    private static DebitWalletCommand BuildCommand(decimal amount = 100_000m)
        => new DebitWalletCommand(
            UserId: 1,
            Amount: amount,
            TransactionType: WalletTransactionType.OrderPayment,
            ReferenceType: WalletReferenceType.Order,
            ReferenceId: 1,
            IdempotencyKey: Guid.NewGuid().ToString());

    [Fact]
    public async Task Handle_WhenWalletHasSufficientBalance_ShouldDebitAndReturnSuccess()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(BuildCommand(100_000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWalletNotFound_ShouldReturnFailure()
    {
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Domain.Wallet.Aggregates.Wallet?)null);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_ShouldReturnFailure()
    {
        var wallet = new WalletBuilder().WithBalance(10m).Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(BuildCommand(amount: 1_000_000m), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnSuccessImmediately()
    {
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _walletRepository.DidNotReceive().GetByUserIdForUpdateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldDecreaseWalletBalance()
    {
        var wallet = new WalletBuilder().WithBalance(500_000m).Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var balanceBefore = wallet.CurrentBalance;

        await _handler.Handle(BuildCommand(200_000m), CancellationToken.None);

        wallet.CurrentBalance.Should().Be(balanceBefore - 200_000m);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldCallSaveChanges()
    {
        var wallet = new WalletBuilder().WithBalance(500_000m).Build();
        _walletRepository.GetByUserIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(wallet);
        _walletRepository.HasIdempotencyKeyAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(BuildCommand(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}