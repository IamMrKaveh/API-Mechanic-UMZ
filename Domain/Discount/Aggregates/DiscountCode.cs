using Domain.Discount.Entities;
using Domain.Discount.Enums;
using Domain.Discount.Events;
using Domain.Discount.Exceptions;
using Domain.Discount.Results;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Aggregates;

public sealed class DiscountCode : AggregateRoot<DiscountCodeId>
{
    private readonly List<DiscountRestriction> _restrictions = new();
    private readonly List<DiscountUsageRecord> _usages = new();

    private DiscountCode()
    { }

    public string Code { get; private set; } = default!;
    public DiscountValue Value { get; private set; } = default!;
    public Money? MaximumDiscountAmount { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool HasStarted => !StartsAt.HasValue || DateTime.UtcNow >= StartsAt.Value;
    public bool HasReachedUsageLimit => UsageLimit.HasValue && UsageCount >= UsageLimit.Value;
    public bool IsRedeemable => IsActive && HasStarted && !IsExpired && !HasReachedUsageLimit;

    public IReadOnlyList<DiscountRestriction> Restrictions => _restrictions.AsReadOnly();
    public IReadOnlyList<DiscountUsageRecord> Usages => _usages.AsReadOnly();

    public static DiscountCode Create(
        DiscountCodeId id,
        string code,
        DiscountValue value,
        Money? maximumDiscountAmount = null,
        int? usageLimit = null,
        DateTime? startsAt = null,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("کد تخفیف الزامی است.");

        if (expiresAt.HasValue && startsAt.HasValue && expiresAt.Value <= startsAt.Value)
            throw new InvalidDiscountException("تاریخ انقضا باید بعد از تاریخ شروع باشد.");

        var discountCode = new DiscountCode
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            Value = value,
            MaximumDiscountAmount = maximumDiscountAmount,
            UsageLimit = usageLimit,
            UsageCount = 0,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        discountCode.RaiseDomainEvent(new DiscountCodeCreatedEvent(
            id, code, value.Type, value.Amount, usageLimit, expiresAt));
        return discountCode;
    }

    public void Update(
        DiscountValue discountValue,
        Money? maximumDiscountAmount,
        int? usageLimit,
        DateTime? startsAt,
        DateTime? expiresAt)
    {
        if (expiresAt.HasValue && startsAt.HasValue && expiresAt.Value <= startsAt.Value)
            throw new InvalidDiscountException("تاریخ انقضا باید بعد از تاریخ شروع باشد.");

        Value = discountValue;
        MaximumDiscountAmount = maximumDiscountAmount;
        UsageLimit = usageLimit;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public DiscountValidation ValidateForApplication(Money orderAmount)
    {
        if (!IsActive)
            return DiscountValidation.Fail("کد تخفیف غیرفعال است.");

        if (!HasStarted)
            return DiscountValidation.Fail("کد تخفیف هنوز فعال نشده است.");

        if (IsExpired)
            return DiscountValidation.Fail("کد تخفیف منقضی شده است.");

        if (HasReachedUsageLimit)
            return DiscountValidation.Fail("سقف استفاده از کد تخفیف پر شده است.");

        foreach (var restriction in _restrictions)
        {
            if (restriction.RestrictionType == DiscountRestrictionType.MinimumOrderAmount)
            {
                if (decimal.TryParse(restriction.RestrictionValue, out var minAmount)
                    && orderAmount.Amount < minAmount)
                    return DiscountValidation.Fail(
                        $"حداقل مبلغ سفارش برای استفاده از این کد تخفیف {minAmount:N0} تومان است.");
            }
        }

        return DiscountValidation.Success();
    }

    public Money CalculateDiscount(Money orderAmount)
    {
        var discountedPrice = Value.Apply(orderAmount);
        var discountAmount = orderAmount.Subtract(discountedPrice);

        if (MaximumDiscountAmount is not null
            && discountAmount.IsGreaterThan(MaximumDiscountAmount))
            discountAmount = MaximumDiscountAmount;

        if (discountAmount.IsGreaterThan(orderAmount))
            discountAmount = orderAmount;

        return discountAmount;
    }

    public DiscountUsageRecord RecordUsage(UserId userId, OrderId orderId, Money discountedAmount)
    {
        if (!IsRedeemable)
            throw new DiscountCodeNotRedeemableException(Id, Code);

        UsageCount++;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var usage = DiscountUsageRecord.Create(
            Id, Code, userId, orderId, discountedAmount.Amount, UsageCount);
        _usages.Add(usage);

        RaiseDomainEvent(new DiscountCodeAppliedEvent(
            Id, Code, userId, orderId, discountedAmount.Amount, UsageCount));
        return usage;
    }

    public void AddRestriction(
        DiscountRestrictionId restrictionId,
        DiscountRestrictionType restrictionType,
        string restrictionValue)
    {
        if (string.IsNullOrWhiteSpace(restrictionValue))
            throw new DomainException("مقدار محدودیت الزامی است.");

        var restriction = DiscountRestriction.Create(
            restrictionId, Id, restrictionType, restrictionValue);
        _restrictions.Add(restriction);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void RemoveRestriction(DiscountRestrictionId restrictionId)
    {
        var restriction = _restrictions.FirstOrDefault(r => r.Id == restrictionId);
        if (restriction is null) return;

        _restrictions.Remove(restriction);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new DiscountCodeActivatedEvent(Id, Code));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new DiscountCodeDeactivatedEvent(Id, Code));
    }
}