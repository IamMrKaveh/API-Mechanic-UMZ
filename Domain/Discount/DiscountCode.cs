namespace Domain.Discount;

public class DiscountCode : AggregateRoot, ISoftDeletable, IActivatable, IAuditable
{
    private readonly List<DiscountRestriction> _restrictions = new();
    private readonly List<DiscountUsage> _usages = new();

    private string _code = null!;
    private decimal _percentage;
    private decimal? _maxDiscountAmount;
    private decimal? _minOrderAmount;
    private int? _usageLimit;
    private int? _maxUsagePerUser;
    private int _usedCount;
    private bool _isActive;
    private DateTime? _expiresAt;
    private DateTime? _startsAt;

    public string Code => _code;
    public decimal Percentage => _percentage;
    public decimal? MaxDiscountAmount => _maxDiscountAmount;
    public decimal? MinOrderAmount => _minOrderAmount;
    public int? UsageLimit => _usageLimit;
    public int? MaxUsagePerUser => _maxUsagePerUser;
    public int UsedCount => _usedCount;
    public bool IsActive => _isActive;
    public DateTime? ExpiresAt => _expiresAt;
    public DateTime? StartsAt => _startsAt;

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public IReadOnlyCollection<DiscountRestriction> Restrictions => _restrictions.AsReadOnly();

    public IReadOnlyCollection<DiscountUsage> Usages => _usages.AsReadOnly();

    // Business Constants
    private const int MinCodeLength = 3;

    private const int MaxCodeLength = 20;
    private const decimal MinPercentage = 0.01m;
    private const decimal MaxPercentage = 100m;

    private DiscountCode()
    { }

    #region Factory Methods

    public static DiscountCode Create(
        string code,
        decimal percentage,
        decimal? maxDiscountAmount = null,
        decimal? minOrderAmount = null,
        int? usageLimit = null,
        DateTime? expiresAt = null,
        DateTime? startsAt = null,
        int? maxUsagePerUser = null)
    {
        var discount = new DiscountCode();

        discount.SetCode(code);
        discount.SetPercentage(percentage);
        discount.SetMaxDiscountAmount(maxDiscountAmount);
        discount.SetMinOrderAmount(minOrderAmount);
        discount.SetUsageLimit(usageLimit);
        discount.SetMaxUsagePerUser(maxUsagePerUser);
        discount.SetDateRange(startsAt, expiresAt);

        discount._usedCount = 0;
        discount._isActive = true;
        discount.CreatedAt = DateTime.UtcNow;

        discount.AddDomainEvent(new Events.DiscountCreatedEvent(discount.Id, discount._code));
        return discount;
    }

    #endregion Factory Methods

    #region Domain Behaviors

    public void Update(
        decimal percentage,
        decimal? maxDiscountAmount,
        decimal? minOrderAmount,
        int? usageLimit,
        bool isActive,
        DateTime? expiresAt,
        DateTime? startsAt = null,
        int? maxUsagePerUser = null)
    {
        EnsureNotDeleted();

        SetPercentage(percentage);
        SetMaxDiscountAmount(maxDiscountAmount);
        SetMinOrderAmount(minOrderAmount);
        SetUsageLimit(usageLimit);
        SetMaxUsagePerUser(maxUsagePerUser);
        SetDateRange(startsAt, expiresAt);

        if (isActive)
            Activate();
        else
            Deactivate();

        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeCode(string newCode)
    {
        EnsureNotDeleted();
        EnsureNotUsed();

        SetCode(newCode);
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Behaviors

    #region Validation (Backward-compatible Tuple + Rich Result)

    /// <summary>
    /// اعتبارسنجی ساده - Tuple برمی‌گرداند (سازگار با کدهای قدیمی)
    /// </summary>
    public (bool IsValid, string? Error) Validate(decimal orderTotal, int userId, int userPreviousUsageCount)
    {
        var result = ValidateForApplication(orderTotal, userId, userPreviousUsageCount);
        return (result.IsValid, result.Error);
    }

    /// <summary>
    /// اعتبارسنجی غنی - DiscountValidation برمی‌گرداند
    /// </summary>
    public DiscountValidation ValidateForApplication(decimal orderTotal, int userId, int userPreviousUsageCount)
    {
        if (!_isActive)
            return DiscountValidation.Invalid("کد تخفیف غیرفعال است.");

        if (IsDeleted)
            return DiscountValidation.Invalid("کد تخفیف حذف شده است.");

        if (!HasStarted())
            return DiscountValidation.Invalid("کد تخفیف هنوز فعال نشده است.");

        if (IsExpired())
            return DiscountValidation.Invalid("کد تخفیف منقضی شده است.");

        if (HasReachedUsageLimit())
            return DiscountValidation.Invalid("ظرفیت استفاده از این کد تخفیف تکمیل شده است.");

        if (!MeetsMinimumOrderAmount(orderTotal))
            return DiscountValidation.Invalid($"حداقل مبلغ سفارش برای این کد تخفیف {_minOrderAmount!.Value:N0} تومان است.");

        if (!CanBeUsedByUser(userId, userPreviousUsageCount))
            return DiscountValidation.Invalid("شما به حداکثر تعداد استفاده مجاز از این کد تخفیف رسیده‌اید.");

        return DiscountValidation.Valid();
    }

    #endregion Validation (Backward-compatible Tuple + Rich Result)

    #region Discount Calculation & Application

    public DiscountApplicationResult TryApply(decimal orderTotal, int userId, int userPreviousUsageCount)
    {
        var validationResult = ValidateForApplication(orderTotal, userId, userPreviousUsageCount);

        if (!validationResult.IsValid)
            return DiscountApplicationResult.Failed(validationResult.Error!);

        var discountAmount = CalculateDiscountAmount(orderTotal);
        return DiscountApplicationResult.Success(discountAmount);
    }

    /// <summary>
    /// محاسبه مبلغ تخفیف بر اساس مبلغ سفارش
    /// </summary>
    public decimal CalculateDiscountAmount(decimal orderTotal)
    {
        if (!CanCalculateDiscount(orderTotal))
            return 0;

        var discountAmount = orderTotal * _percentage / 100;

        if (_maxDiscountAmount.HasValue)
        {
            discountAmount = Math.Min(discountAmount, _maxDiscountAmount.Value);
        }

        return Math.Round(discountAmount, 0);
    }

    public Money CalculateDiscountMoney(Money orderTotal)
    {
        var amount = CalculateDiscountAmount(orderTotal.Amount);
        return Money.FromDecimal(amount);
    }

    #endregion Discount Calculation & Application

    #region Usage Management

    /// <summary>
    /// افزایش شمارنده استفاده (برای سناریوهای ساده بدون ثبت جزئیات)
    /// </summary>
    public void IncrementUsage()
    {
        EnsureNotDeleted();
        EnsureCanBeUsed();
        _usedCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ثبت استفاده کامل از تخفیف با ایجاد DiscountUsage
    /// </summary>
    public DiscountUsage RecordUsage(int userId, int orderId, Money discountAmount)
    {
        EnsureNotDeleted();
        EnsureCanBeUsed();

        var usage = DiscountUsage.Record(userId, Id, orderId, discountAmount);
        _usages.Add(usage);
        IncrementUsageCount();

        AddDomainEvent(new Events.DiscountAppliedEvent(Id, userId, orderId));
        return usage;
    }

    public void CancelUsage(int orderId)
    {
        var usage = _usages.FirstOrDefault(u => u.OrderId == orderId && u.IsConfirmed);
        if (usage != null)
        {
            usage.Cancel();
            DecrementUsageCount();

            AddDomainEvent(new Events.DiscountUsageCancelledEvent(Id, orderId));
        }
    }

    public void ConfirmUsage(int orderId)
    {
        var usage = _usages.FirstOrDefault(u => u.OrderId == orderId && !u.IsConfirmed && !u.IsCancelled);
        if (usage == null)
            throw new DomainException("استفاده‌ای برای تأیید یافت نشد.");

        usage.Confirm();
    }

    private void IncrementUsageCount()
    {
        _usedCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    private void DecrementUsageCount()
    {
        if (_usedCount > 0)
        {
            _usedCount--;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private void EnsureCanBeUsed()
    {
        if (_usageLimit.HasValue && _usedCount >= _usageLimit.Value)
        {
            throw new Exceptions.DiscountUsageLimitExceededException(_code, _usageLimit.Value, _usedCount);
        }
    }

    #endregion Usage Management

    #region Restriction Management

    public void AddCategoryRestriction(int categoryId)
    {
        AddRestriction(DiscountRestrictionType.Category, categoryId);
    }

    public void AddProductRestriction(int productId)
    {
        AddRestriction(DiscountRestrictionType.Product, productId);
    }

    public void AddUserRestriction(int userId)
    {
        AddRestriction(DiscountRestrictionType.User, userId);
    }

    public void AddBrandRestriction(int brandId)
    {
        AddRestriction(DiscountRestrictionType.Brand, brandId);
    }

    /// <summary>
    /// افزودن محدودیت با نوع رشته‌ای (backward compatibility)
    /// </summary>
    public void AddRestriction(string restrictionType, int? entityId)
    {
        EnsureNotDeleted();

        if (!System.Enum.TryParse<DiscountRestrictionType>(restrictionType, true, out var type))
            throw new DomainException($"نوع محدودیت '{restrictionType}' معتبر نیست.");

        if (entityId.HasValue && HasRestriction(type, entityId.Value))
            return;

        var restriction = DiscountRestriction.Create(Id, type, entityId);
        _restrictions.Add(restriction);
        UpdatedAt = DateTime.UtcNow;
    }

    private void AddRestriction(DiscountRestrictionType type, int entityId)
    {
        EnsureNotDeleted();

        if (HasRestriction(type, entityId))
            return;

        var restriction = DiscountRestriction.Create(Id, type, entityId);
        _restrictions.Add(restriction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveRestriction(int restrictionId)
    {
        var restriction = _restrictions.FirstOrDefault(r => r.Id == restrictionId);
        if (restriction != null)
        {
            _restrictions.Remove(restriction);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearRestrictions()
    {
        _restrictions.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasRestriction(DiscountRestrictionType type, int entityId)
    {
        return _restrictions.Any(r => r.Type == type && r.EntityId == entityId);
    }

    public bool IsRestrictedToCategory(int categoryId) => HasRestriction(DiscountRestrictionType.Category, categoryId);

    public bool IsRestrictedToProduct(int productId) => HasRestriction(DiscountRestrictionType.Product, productId);

    public bool IsRestrictedToUser(int userId) => HasRestriction(DiscountRestrictionType.User, userId);

    public bool IsRestrictedToBrand(int brandId) => HasRestriction(DiscountRestrictionType.Brand, brandId);

    public bool HasAnyRestrictions() => _restrictions.Any();

    public IEnumerable<int> GetRestrictedCategoryIds() =>
        _restrictions.Where(r => r.Type == DiscountRestrictionType.Category).Select(r => r.EntityId!.Value);

    public IEnumerable<int> GetRestrictedProductIds() =>
        _restrictions.Where(r => r.Type == DiscountRestrictionType.Product).Select(r => r.EntityId!.Value);

    #endregion Restriction Management

    #region Activation & Lifecycle

    public void Activate()
    {
        if (_isActive) return;
        EnsureNotDeleted();

        _isActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!_isActive) return;

        _isActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        _isActive = false;

        AddDomainEvent(new Events.DiscountDeletedEvent(Id, deletedBy));
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsExpired()
    {
        if (!IsCurrentlyValid()) return;

        _isActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new Events.DiscountExpiredEvent(Id));
    }

    #endregion Activation & Lifecycle

    #region Query Methods

    public bool IsExpired() => _expiresAt.HasValue && _expiresAt.Value < DateTime.UtcNow;

    public bool HasStarted() => !_startsAt.HasValue || _startsAt.Value <= DateTime.UtcNow;

    public bool IsCurrentlyValid() => _isActive && !IsDeleted && HasStarted() && !IsExpired();

    public bool HasReachedUsageLimit() => _usageLimit.HasValue && _usedCount >= _usageLimit.Value;

    /// <summary>
    /// آیا کاربر مشخص می‌تواند از این کد استفاده کند
    /// </summary>
    public bool CanBeUsedByUser(int userId, int userPreviousUsageCount)
    {
        return !HasReachedUserUsageLimit(userPreviousUsageCount);
    }

    public bool HasReachedUserUsageLimit(int userUsageCount) =>
        _maxUsagePerUser.HasValue && userUsageCount >= _maxUsagePerUser.Value;

    public bool MeetsMinimumOrderAmount(decimal orderTotal) =>
        !_minOrderAmount.HasValue || orderTotal >= _minOrderAmount.Value;

    public int RemainingUsage() => _usageLimit.HasValue ? Math.Max(0, _usageLimit.Value - _usedCount) : int.MaxValue;

    public TimeSpan? TimeUntilExpiry() => _expiresAt.HasValue ? _expiresAt.Value - DateTime.UtcNow : null;

    public TimeSpan? TimeUntilStart() => _startsAt.HasValue && !HasStarted() ? _startsAt.Value - DateTime.UtcNow : null;

    public decimal GetEffectivePercentage(decimal orderTotal)
    {
        var discountAmount = CalculateDiscountAmount(orderTotal);
        if (orderTotal == 0) return 0;
        return Math.Round((discountAmount / orderTotal) * 100, 2);
    }

    #endregion Query Methods

    #region Private Domain Invariants

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("کد تخفیف حذف شده است.");
    }

    private void EnsureNotUsed()
    {
        if (_usedCount > 0)
        {
            throw new DomainException("امکان تغییر کد تخفیف استفاده شده وجود ندارد.");
        }
    }

    private bool CanCalculateDiscount(decimal orderTotal)
    {
        return _isActive && !IsDeleted && HasStarted() && !IsExpired() && MeetsMinimumOrderAmount(orderTotal);
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("کد تخفیف الزامی است.");

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length < MinCodeLength)
            throw new DomainException($"کد تخفیف باید حداقل {MinCodeLength} کاراکتر باشد.");

        if (normalized.Length > MaxCodeLength)
            throw new DomainException($"کد تخفیف نمی‌تواند بیش از {MaxCodeLength} کاراکتر باشد.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-_]+$"))
            throw new DomainException("کد تخفیف فقط می‌تواند شامل حروف، اعداد، خط تیره و زیرخط باشد.");

        _code = normalized;
    }

    private void SetPercentage(decimal percentage)
    {
        if (percentage < MinPercentage)
            throw new DomainException("درصد تخفیف باید بزرگتر از صفر باشد.");

        if (percentage > MaxPercentage)
            throw new DomainException("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");

        _percentage = percentage;
    }

    private void SetMaxDiscountAmount(decimal? maxDiscountAmount)
    {
        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
            throw new DomainException("حداکثر مبلغ تخفیف باید بزرگتر از صفر باشد.");

        _maxDiscountAmount = maxDiscountAmount;
    }

    private void SetMinOrderAmount(decimal? minOrderAmount)
    {
        if (minOrderAmount.HasValue && minOrderAmount.Value <= 0)
            throw new DomainException("حداقل مبلغ سفارش باید بزرگتر از صفر باشد.");

        _minOrderAmount = minOrderAmount;
    }

    private void SetUsageLimit(int? usageLimit)
    {
        if (usageLimit.HasValue && usageLimit.Value <= 0)
            throw new DomainException("محدودیت استفاده باید بزرگتر از صفر باشد.");

        _usageLimit = usageLimit;
    }

    private void SetMaxUsagePerUser(int? maxUsagePerUser)
    {
        if (maxUsagePerUser.HasValue && maxUsagePerUser.Value <= 0)
            throw new DomainException("حداکثر استفاده هر کاربر باید بزرگتر از صفر باشد.");

        _maxUsagePerUser = maxUsagePerUser;
    }

    private void SetDateRange(DateTime? startsAt, DateTime? expiresAt)
    {
        if (startsAt.HasValue && expiresAt.HasValue && startsAt.Value >= expiresAt.Value)
            throw new DomainException("تاریخ شروع باید قبل از تاریخ پایان باشد.");

        _startsAt = startsAt;
        _expiresAt = expiresAt;
    }

    #endregion Private Domain Invariants
}