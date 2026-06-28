using Application.Payment.Features.Shared;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentMethodQueryService
{
    Task<PaymentMethodDto?> GetByIdAsync(PaymentMethodId id, CancellationToken ct);

    Task<IReadOnlyList<PaymentMethodListItemDto>> GetAllAsync(bool includeInactive, bool includeDeleted, CancellationToken ct);

    Task<IReadOnlyList<AvailablePaymentMethodDto>> GetActiveAsync(decimal orderAmount, CancellationToken ct);
}