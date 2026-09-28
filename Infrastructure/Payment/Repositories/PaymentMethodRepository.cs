using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Repositories;

public sealed class PaymentMethodRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<PaymentMethod, PaymentMethodId>(context), IPaymentMethodRepository
{
    public async Task<ICollection<PaymentMethod>> GetAllAsync(
        bool includeInactive = false,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? Context.PaymentMethods.IgnoreQueryFilters().AsQueryable()
            : Context.PaymentMethods.AsQueryable();

        if (!includeInactive && !includeDeleted)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public Task<PaymentMethod?> GetByCodeAsync(PaymentMethodCode code, CancellationToken ct = default)
        => Context.PaymentMethods.FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task<bool> ExistsByNameAsync(
        PaymentMethodName name,
        PaymentMethodId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = Context.PaymentMethods.Where(p => p.Name == name);
        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId);
        return query.AnyAsync(ct);
    }

    public Task<bool> ExistsByCodeAsync(
        PaymentMethodCode code,
        PaymentMethodId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = Context.PaymentMethods.Where(p => p.Code == code);
        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId);
        return query.AnyAsync(ct);
    }

}