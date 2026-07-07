using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletWithdrawalRepository
{
    Task AddAsync(WalletWithdrawalRequest withdrawal, CancellationToken ct = default);

    void Update(WalletWithdrawalRequest withdrawal);

    Task<WalletWithdrawalRequest?> GetByIdAsync(
        WalletWithdrawalRequestId id,
        CancellationToken ct = default);

    Task<WalletWithdrawalRequest?> GetByIdForUpdateAsync(
        WalletWithdrawalRequestId id,
        CancellationToken ct = default);

    Task<int> CountByUserAndStatusAsync(
        UserId userId,
        WithdrawalStatus status,
        CancellationToken ct = default);
}