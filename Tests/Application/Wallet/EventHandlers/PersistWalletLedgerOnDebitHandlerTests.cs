using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Events;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class PersistWalletLedgerOnDebitHandlerTests
{
    private readonly IWalletLedgerRepository _ledgerRepository = Substitute.For<IWalletLedgerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly PersistWalletLedgerOnDebitHandler _sut;

    public PersistWalletLedgerOnDebitHandlerTests()
    {
        _sut = new PersistWalletLedgerOnDebitHandler(_ledgerRepository, _unitOfWork, _auditService);
    }

    private static WalletDebitedEvent BuildDebitEvent(
        string? idempotencyKey = "idem-001",
        string description = "system debit",
        string referenceId = "ref-001",
        decimal amount = 25_000m,
        decimal newBalance = 75_000m)
    {
        return new WalletDebitedEvent(
            WalletId.NewId(),
            UserId.NewId(),
            Money.Create(amount),
            Money.Create(newBalance),
            description,
            referenceId,
            idempotencyKey);
    }

    private static DomainEventNotification<WalletDebitedEvent> Wrap(WalletDebitedEvent evt) =>
        new(evt);

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyPersisted_SkipsAddAndSave()
    {
        var evt = BuildDebitEvent(idempotencyKey: "existing-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "existing-key", Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _ledgerRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await _auditService.DidNotReceiveWithAnyArgs().LogInformationAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenIdempotencyKeyIsEmpty_SkipsIdempotencyCheckAndPersists(string? idempotencyKey)
    {
        var evt = BuildDebitEvent(idempotencyKey: idempotencyKey);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _ledgerRepository.DidNotReceiveWithAnyArgs()
            .HasIdempotencyKeyAsync(default(UserId)!, default(string)!, default);
        await _ledgerRepository.Received(1).AddAsync(Arg.Any<WalletLedgerEntry>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotDuplicate_AddsLedgerEntryAndSavesChanges()
    {
        var evt = BuildDebitEvent(idempotencyKey: "fresh-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "fresh-key", Arg.Any<CancellationToken>())
            .Returns(false);

        WalletLedgerEntry? captured = null;
        await _ledgerRepository.AddAsync(
            Arg.Do<WalletLedgerEntry>(e => captured = e),
            Arg.Any<CancellationToken>());

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _ledgerRepository.Received(1).AddAsync(Arg.Any<WalletLedgerEntry>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        captured.ShouldNotBeNull();
        captured!.WalletId.ShouldBe(evt.WalletId);
        captured.OwnerId.ShouldBe(evt.OwnerId);
        captured.Amount.Amount.ShouldBe(evt.Amount.Amount);
        captured.BalanceAfter.Amount.ShouldBe(evt.NewBalance.Amount);
        captured.IdempotencyKey.ShouldBe("fresh-key");
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolationOnKnownIndex_LogsInformationAndSwallows()
    {
        var evt = BuildDebitEvent(idempotencyKey: "race-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "race-key", Arg.Any<CancellationToken>())
            .Returns(false);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "update failed",
                new Exception("duplicate key value violates unique constraint IX_WalletLedgerEntries_IdempotencyKey")));

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("WalletLedger debit already persisted")
                             && s.Contains(evt.WalletId.Value.ToString())
                             && s.Contains("race-key")),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolationOnDuplicateKeyText_LogsInformationAndSwallows()
    {
        var evt = BuildDebitEvent(idempotencyKey: "race-key-2");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "race-key-2", Arg.Any<CancellationToken>())
            .Returns(false);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "update failed",
                new Exception("23505: duplicate key value")));

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _auditService.Received(1).LogInformationAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionUnrelated_LogsErrorAndRethrows()
    {
        var evt = BuildDebitEvent(idempotencyKey: "some-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "some-key", Arg.Any<CancellationToken>())
            .Returns(false);
        var unrelated = new DbUpdateException(
            "update failed",
            new Exception("connection reset by peer"));
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(unrelated);

        var ex = await Should.ThrowAsync<DbUpdateException>(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        ex.ShouldBe(unrelated);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Failed to persist wallet debit ledger")
                             && s.Contains(evt.WalletId.Value.ToString())
                             && s.Contains("some-key")),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogInformationAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenGenericExceptionThrown_LogsErrorAndRethrows()
    {
        var evt = BuildDebitEvent(idempotencyKey: "boom-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "boom-key", Arg.Any<CancellationToken>())
            .Returns(false);
        var boom = new InvalidOperationException("boom");
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(boom);

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        ex.Message.ShouldBe("boom");
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Failed to persist wallet debit ledger")
                             && s.Contains("boom")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAddAsyncThrows_LogsErrorAndRethrows()
    {
        var evt = BuildDebitEvent(idempotencyKey: "add-fail");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "add-fail", Arg.Any<CancellationToken>())
            .Returns(false);
        _ledgerRepository
            .AddAsync(Arg.Any<WalletLedgerEntry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("cannot add"));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAllDependencies()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildDebitEvent(idempotencyKey: "ct-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "ct-key", cts.Token)
            .Returns(false);

        await _sut.Handle(Wrap(evt), cts.Token);

        await _ledgerRepository.Received(1)
            .HasIdempotencyKeyAsync(evt.OwnerId, "ct-key", cts.Token);
        await _ledgerRepository.Received(1)
            .AddAsync(Arg.Any<WalletLedgerEntry>(), cts.Token);
        await _unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }
}
