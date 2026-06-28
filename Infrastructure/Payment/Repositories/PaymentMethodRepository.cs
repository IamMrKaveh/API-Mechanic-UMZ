using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Repositories;

public sealed class PaymentMethodRepository(DBContext context) : IPaymentMethodRepository
{
    public async Task<ICollection<PaymentMethod>> GetAllAsync(
        bool includeInactive = false,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? context.PaymentMethods.IgnoreQueryFilters().AsQueryable()
            : context.PaymentMethods.AsQueryable();

        if (!includeInactive && !includeDeleted)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken ct = default)
        => context.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PaymentMethod?> GetByCodeAsync(PaymentMethodCode code, CancellationToken ct = default)
        => context.PaymentMethods.FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task<bool> ExistsByNameAsync(
        PaymentMethodName name,
        PaymentMethodId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.PaymentMethods.Where(p => p.Name == name);
        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId);
        return query.AnyAsync(ct);
    }

    public Task<bool> ExistsByCodeAsync(
        PaymentMethodCode code,
        PaymentMethodId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.PaymentMethods.Where(p => p.Code == code);
        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId);
        return query.AnyAsync(ct);
    }

    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken ct = default)
        => await context.PaymentMethods.AddAsync(paymentMethod, ct);

    public void Update(PaymentMethod paymentMethod)
        => context.PaymentMethods.Update(paymentMethod);
}