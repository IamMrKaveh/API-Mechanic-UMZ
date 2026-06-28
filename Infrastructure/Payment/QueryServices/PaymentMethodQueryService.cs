using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.QueryServices;

public sealed class PaymentMethodQueryService(DBContext context) : IPaymentMethodQueryService
{
    public async Task<PaymentMethodDto?> GetByIdAsync(PaymentMethodId id, CancellationToken ct)
    {
        var method = await context.PaymentMethods
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (method is null) return null;

        return new PaymentMethodDto
        {
            Id = method.Id.Value,
            Name = method.Name.Value,
            Code = method.Code.Value,
            Description = method.Description,
            IconUrl = method.IconUrl,
            FeeAmount = method.Fee.Amount.Amount,
            FeePercentage = method.Fee.Percentage,
            IsActive = method.IsActive,
            SortOrder = method.SortOrder,
            CreatedAt = method.CreatedAt,
            UpdatedAt = method.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<PaymentMethodListItemDto>> GetAllAsync(
        bool includeInactive,
        bool includeDeleted,
        CancellationToken ct)
    {
        var query = includeDeleted
            ? context.PaymentMethods.AsNoTracking().IgnoreQueryFilters()
            : context.PaymentMethods.AsNoTracking();

        if (!includeInactive && !includeDeleted)
            query = query.Where(p => p.IsActive);

        var rows = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        return rows.Select(m => new PaymentMethodListItemDto
        {
            Id = m.Id.Value,
            Name = m.Name.Value,
            Code = m.Code.Value,
            IconUrl = m.IconUrl,
            FeeAmount = m.Fee.Amount.Amount,
            FeePercentage = m.Fee.Percentage,
            IsActive = m.IsActive,
            IsDeleted = m.IsDeleted,
            SortOrder = m.SortOrder
        }).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<AvailablePaymentMethodDto>> GetActiveAsync(
        decimal orderAmount,
        CancellationToken ct)
    {
        var rows = await context.PaymentMethods
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        var orderTotal = Money.FromDecimal(orderAmount);

        return rows.Select(m => new AvailablePaymentMethodDto
        {
            Id = m.Id.Value,
            Name = m.Name.Value,
            Code = m.Code.Value,
            IconUrl = m.IconUrl,
            Description = m.Description,
            Fee = m.CalculateFee(orderTotal).Amount,
            SortOrder = m.SortOrder
        }).ToList().AsReadOnly();
    }
}