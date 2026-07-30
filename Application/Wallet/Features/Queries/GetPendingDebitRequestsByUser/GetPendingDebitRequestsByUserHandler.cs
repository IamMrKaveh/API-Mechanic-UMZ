using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Queries.GetPendingDebitRequestsByUser;

public sealed class GetPendingDebitRequestsByUserHandler(
    IWalletDebitRequestRepository debitRequestRepository)
    : IQueryHandler<GetPendingDebitRequestsByUserQuery, IReadOnlyList<WalletDebitRequestDto>>
{
    private static readonly TimeSpan ExpiringSoonThreshold = TimeSpan.FromHours(6);

    public async Task<ServiceResult<IReadOnlyList<WalletDebitRequestDto>>> Handle(
        GetPendingDebitRequestsByUserQuery request,
        CancellationToken ct)
    {
        var ownerId = UserId.From(request.UserId);
        var items = await debitRequestRepository.GetPendingByOwnerAsync(ownerId, ct);
        var now = DateTime.UtcNow;

        var dtos = items.Select(r => new WalletDebitRequestDto(
            r.Id.Value,
            r.WalletId.Value,
            r.OwnerId.Value,
            r.Amount.Amount,
            r.Amount.Currency,
            r.Reason,
            r.Description,
            r.Status.ToString(),
            r.CreatedAt,
            r.ExpiresAt,
            r.RespondedAt,
            r.RejectionReason,
            r.RequestedBy.Value,
            r.Status == WalletDebitRequestStatus.Pending && (r.ExpiresAt - now) <= ExpiringSoonThreshold
        )).ToList().AsReadOnly();

        return ServiceResult<IReadOnlyList<WalletDebitRequestDto>>.Success(dtos);
    }
}
