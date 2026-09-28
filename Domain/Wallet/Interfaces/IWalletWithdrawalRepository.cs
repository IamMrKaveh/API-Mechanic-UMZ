using Domain.Common.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletWithdrawalRepository : IRepository<WalletWithdrawalRequest, WalletWithdrawalRequestId>
{
    Task<WalletWithdrawalRequest?> GetByIdForUpdateAsync(
        WalletWithdrawalRequestId id,
        CancellationToken ct = default);

    Task<int> CountByUserAndStatusAsync(
        UserId userId,
        WalletWithdrawalStatus status,
        CancellationToken ct = default);
}