using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Interfaces;

public interface IPaymentMethodRepository
{
    Task<ICollection<PaymentMethod>> GetAllAsync(bool includeInactive = false, bool includeDeleted = false, CancellationToken ct = default);

    Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken ct = default);

    Task<PaymentMethod?> GetByCodeAsync(PaymentMethodCode code, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(PaymentMethodName name, PaymentMethodId? excludeId = null, CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(PaymentMethodCode code, PaymentMethodId? excludeId = null, CancellationToken ct = default);

    Task AddAsync(PaymentMethod paymentMethod, CancellationToken ct = default);

    void Update(PaymentMethod paymentMethod);
}