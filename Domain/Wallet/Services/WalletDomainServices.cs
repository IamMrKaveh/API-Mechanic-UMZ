namespace Domain.Wallet.Services;

public sealed class WalletDomainService
{
    public static WalletTransactionResult Credit(
        Aggregates.Wallet wallet,
        WalletLedgerEntryId ledgerEntryId,
        Money amount,
        string description,
        string referenceId,
        string? idempotencyKey = null)
    {
        Guard.Against.Null(wallet, nameof(wallet));
        Guard.Against.Null(ledgerEntryId, nameof(ledgerEntryId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        wallet.Credit(amount, description, referenceId);

        var ledgerEntry = WalletLedgerEntry.Create(
            ledgerEntryId,
            wallet.Id,
            wallet.OwnerId,
            amount,
            wallet.Balance,
            WalletTransactionType.Credit,
            description,
            referenceId,
            idempotencyKey);

        return WalletTransactionResult.Success(wallet.Id, wallet.Balance, ledgerEntry);
    }

    public static WalletTransactionResult Debit(
        Aggregates.Wallet wallet,
        WalletLedgerEntryId ledgerEntryId,
        Money amount,
        string description,
        string referenceId,
        string? idempotencyKey = null)
    {
        Guard.Against.Null(wallet, nameof(wallet));
        Guard.Against.Null(ledgerEntryId, nameof(ledgerEntryId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        wallet.Debit(amount, description, referenceId);

        var ledgerEntry = WalletLedgerEntry.Create(
            ledgerEntryId,
            wallet.Id,
            wallet.OwnerId,
            amount,
            wallet.Balance,
            WalletTransactionType.Debit,
            description,
            referenceId,
            idempotencyKey);

        return WalletTransactionResult.Success(wallet.Id, wallet.Balance, ledgerEntry);
    }

    public static WalletReservationResult Reserve(
        Aggregates.Wallet wallet,
        WalletReservationId reservationId,
        Money amount,
        string purpose,
        DateTime? expiresAt = null)
    {
        Guard.Against.Null(wallet, nameof(wallet));
        Guard.Against.Null(reservationId, nameof(reservationId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.NullOrWhiteSpace(purpose, nameof(purpose));

        if (!wallet.HasSufficientBalance(amount))
            return WalletReservationResult.InsufficientBalance(wallet.Id, amount, wallet.AvailableBalance);

        var reservation = wallet.CreateReservation(reservationId, amount, purpose);
        return WalletReservationResult.Success(wallet.Id, reservation);
    }

    public static WalletTransactionResult ConfirmReservation(
        Aggregates.Wallet wallet,
        WalletLedgerEntryId ledgerEntryId,
        WalletReservationId reservationId,
        string description,
        string referenceId)
    {
        Guard.Against.Null(wallet, nameof(wallet));
        Guard.Against.Null(ledgerEntryId, nameof(ledgerEntryId));
        Guard.Against.Null(reservationId, nameof(reservationId));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        var reservation = wallet.ActiveReservations.FirstOrDefault(r => r.Id == reservationId);
        if (reservation is null)
            throw new WalletReservationNotFoundException(reservationId);

        var reservedAmount = reservation.Amount;

        wallet.ConfirmReservation(reservationId, description, referenceId);

        var ledgerEntry = WalletLedgerEntry.Create(
            ledgerEntryId,
            wallet.Id,
            wallet.OwnerId,
            reservedAmount,
            wallet.Balance,
            WalletTransactionType.ReservationConfirmed,
            description,
            referenceId);

        return WalletTransactionResult.Success(wallet.Id, wallet.Balance, ledgerEntry);
    }

    public static Result ReleaseReservation(
        Aggregates.Wallet wallet,
        WalletReservationId reservationId)
    {
        Guard.Against.Null(wallet, nameof(wallet));
        Guard.Against.Null(reservationId, nameof(reservationId));

        if (!wallet.HasActiveReservation(reservationId))
            return Result.Failure($"رزرو با شناسه '{reservationId}' یافت نشد یا فعال نیست.");

        wallet.ReleaseReservation(reservationId);
        return Result.Success();
    }

    public static WalletBalanceResult CalculateBalance(Aggregates.Wallet wallet)
    {
        Guard.Against.Null(wallet, nameof(wallet));

        return new WalletBalanceResult(
            wallet.Id,
            wallet.OwnerId,
            wallet.Balance,
            wallet.ReservedBalance,
            wallet.AvailableBalance,
            wallet.IsActive);
    }

    public static Result ValidateTransfer(
        Aggregates.Wallet sourceWallet,
        Aggregates.Wallet destinationWallet,
        Money amount)
    {
        Guard.Against.Null(sourceWallet, nameof(sourceWallet));
        Guard.Against.Null(destinationWallet, nameof(destinationWallet));
        Guard.Against.Null(amount, nameof(amount));

        if (!sourceWallet.IsActive)
            return Result.Failure("کیف پول مبدأ غیرفعال است.");

        if (!destinationWallet.IsActive)
            return Result.Failure("کیف پول مقصد غیرفعال است.");

        if (sourceWallet.Id == destinationWallet.Id)
            return Result.Failure("انتقال به همان کیف پول امکان‌پذیر نیست.");

        if (!sourceWallet.HasSufficientBalance(amount))
            return Result.Failure($"موجودی کافی نیست. موجودی قابل دسترس: {sourceWallet.AvailableBalance.ToTomanString()}");

        return Result.Success();
    }
}