using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Aggregates;

public sealed class PaymentMethod : AggregateRoot<PaymentMethodId>, IActivatable, IAuditable, ISoftDeletable
{
    public PaymentMethodName Name { get; private set; } = null!;
    public PaymentMethodCode Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public PaymentMethodFee Fee { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private PaymentMethod()
    { }

    public static PaymentMethod Create(
        PaymentMethodName name,
        PaymentMethodCode code,
        PaymentMethodFee fee,
        string? description = null,
        string? iconUrl = null,
        int sortOrder = 0)
    {
        Guard.Against.Null(name, nameof(name));
        Guard.Against.Null(code, nameof(code));
        Guard.Against.Null(fee, nameof(fee));

        var id = PaymentMethodId.NewId();
        var method = new PaymentMethod
        {
            Id = id,
            Name = name,
            Code = code,
            Fee = fee,
            Description = description?.Trim(),
            IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        method.RaiseDomainEvent(new PaymentMethodCreatedEvent(id, name, code));
        return method;
    }

    public void Update(
        PaymentMethodName name,
        PaymentMethodFee fee,
        string? description,
        string? iconUrl,
        int sortOrder)
    {
        Guard.Against.Null(name, nameof(name));
        Guard.Against.Null(fee, nameof(fee));

        Name = name;
        Fee = fee;
        Description = description?.Trim();
        IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new PaymentMethodUpdatedEvent(Id, name));
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentMethodActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentMethodDeactivatedEvent(Id));
    }

    public void RequestDeletion(UserId? deletedBy = null)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy?.Value;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentMethodDeletedEvent(Id, deletedBy));
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public Money CalculateFee(Money orderTotal) => Fee.CalculateFor(orderTotal);
}