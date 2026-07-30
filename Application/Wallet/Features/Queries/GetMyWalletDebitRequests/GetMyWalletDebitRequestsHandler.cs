using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;

namespace Application.Wallet.Features.Queries.GetMyWalletDebitRequests;

public sealed class GetMyWalletDebitRequestsHandler(
    IWalletDebitRequestRepository debitRequestRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyWalletDebitRequestsQuery, IReadOnlyList<WalletDebitRequestDto>>
{
    private static readonly TimeSpan ExpiringSoonThreshold = TimeSpan.FromHours(6);

    public async Task<ServiceResult<IReadOnlyList<WalletDebitRequestDto>>> Handle(
        GetMyWalletDebitRequestsQuery request,
        CancellationToken ct)
    {
        var ownerId = UserId.From(currentUserService.UserId.Value);

        var items = request.Status.HasValue
            ? await debitRequestRepository.GetByOwnerAsync(ownerId, request.Status.Value, ct)
            : await debitRequestRepository.GetByOwnerAsync(ownerId, null, ct);

        var now = DateTime.UtcNow;
        var dtos = items.Select(x => Map(x, now)).ToList().AsReadOnly();

        return ServiceResult<IReadOnlyList<WalletDebitRequestDto>>.Success(dtos);
    }

    private static WalletDebitRequestDto Map(WalletDebitRequest r, DateTime now)
    {
        var isExpiringSoon = r.Status == WalletDebitRequestStatus.Pending
            && (r.ExpiresAt - now) <= ExpiringSoonThreshold;

        return new WalletDebitRequestDto(
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
            isExpiringSoon);
    }
}
