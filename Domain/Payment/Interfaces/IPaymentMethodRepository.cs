using Domain.Common.Interfaces;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Interfaces;

public interface IPaymentMethodRepository : IRepository<PaymentMethod, PaymentMethodId>
{
    Task<ICollection<PaymentMethod>> GetAllAsync(bool includeInactive = false, bool includeDeleted = false, CancellationToken ct = default);

    Task<PaymentMethod?> GetByCodeAsync(PaymentMethodCode code, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(PaymentMethodName name, PaymentMethodId? excludeId = null, CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(PaymentMethodCode code, PaymentMethodId? excludeId = null, CancellationToken ct = default);
}