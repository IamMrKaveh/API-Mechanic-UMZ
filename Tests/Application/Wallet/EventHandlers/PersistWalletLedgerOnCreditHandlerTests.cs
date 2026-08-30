using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Events;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public class PersistWalletLedgerOnCreditHandlerTests
{
    private readonly IWalletLedgerRepository _ledgerRepository = Substitute.For<IWalletLedgerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly PersistWalletLedgerOnCreditHandler _sut;

    public PersistWalletLedgerOnCreditHandlerTests()
    {
        _sut = new PersistWalletLedgerOnCreditHandler(_ledgerRepository, _unitOfWork, _auditService);
    }

    private static WalletCreditedEvent BuildEvent(string? idempotencyKey = "idem-key-1") => new(
        WalletId.NewId(),
        UserId.NewId(),
        Money.Create(10_000m, "IRT"),
        Money.Create(30_000m, "IRT"),
        description: "شارژ حساب",
        referenceId: Guid.NewGuid().ToString("N"),
        idempotencyKey: idempotencyKey);

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyExists_DoesNotAddOrSave()
    {
        var evt = BuildEvent("dup-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "dup-key", Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None);

        await _ledgerRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyIsNull_SkipsIdempotencyCheckAndPersists()
    {
        var evt = BuildEvent(idempotencyKey: null);

        await _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None);

        await _ledgerRepository.DidNotReceiveWithAnyArgs().HasIdempotencyKeyAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _ledgerRepository.Received(1).AddAsync(Arg.Any<WalletLedgerEntry>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyIsWhitespace_SkipsIdempotencyCheckAndPersists()
    {
        var evt = BuildEvent(idempotencyKey: "   ");

        await _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None);

        await _ledgerRepository.DidNotReceiveWithAnyArgs().HasIdempotencyKeyAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _ledgerRepository.Received(1).AddAsync(
            Arg.Any<WalletLedgerEntry>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotDuplicate_AddsLedgerEntryDerivedFromEvent()
    {
        var evt = BuildEvent("fresh-key");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "fresh-key", Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None);

        await _ledgerRepository.Received(1).AddAsync(
            Arg.Is<WalletLedgerEntry>(e =>
                e!.WalletId == evt.WalletId &&
                e.OwnerId == evt.OwnerId &&
                e.Amount == evt.Amount &&
                e.BalanceAfter == evt.NewBalance &&
                e.IdempotencyKey == evt.IdempotencyKey),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveThrowsUniqueConstraintDbUpdateException_LogsInformationAndSwallows()
    {
        var evt = BuildEvent("k1");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "k1", Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DbUpdateException(
                "outer",
                new Exception("duplicate key value violates unique constraint IX_WalletLedgerEntries_IdempotencyKey")));

        await Should.NotThrowAsync(() =>
            _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None));

        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(m => m!.Contains("idempotency", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveThrowsUnrelatedDbUpdateException_LogsErrorAndRethrows()
    {
        var evt = BuildEvent("k2");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "k2", Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DbUpdateException(
                "outer",
                new Exception("connection reset by peer")));

        await Should.ThrowAsync<DbUpdateException>(() =>
            _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None));

        await _auditService.Received(1).LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveThrowsGenericException_LogsErrorAndRethrows()
    {
        var evt = BuildEvent("k3");
        _ledgerRepository
            .HasIdempotencyKeyAsync(evt.OwnerId, "k3", Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.Handle(new DomainEventNotification<WalletCreditedEvent>(evt), CancellationToken.None));

        await _auditService.Received(1).LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
