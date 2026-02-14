namespace Domain.Order;

public class OrderStatus : BaseEntity, ISoftDeletable, IActivatable
{
    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; }
    public bool AllowCancel { get; private set; }
    public bool AllowEdit { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public ICollection<Order> Orders { get; private set; } = new List<Order>();

    // وضعیت‌های از پیش تعریف شده
    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
        public const string Returned = "Returned";
        public const string Refunded = "Refunded";
    }

    private OrderStatus()
    { }

    #region Factory Methods

    public static OrderStatus Create(
        string name,
        string displayName,
        string? icon = null,
        string? color = null,
        int sortOrder = 0,
        bool allowCancel = false,
        bool allowEdit = false)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));

        return new OrderStatus
        {
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Icon = icon?.Trim(),
            Color = color?.Trim(),
            SortOrder = sortOrder,
            AllowCancel = allowCancel,
            AllowEdit = allowEdit,
            IsActive = true,
            IsDefault = false
        };
    }

    public static OrderStatus CreateDefault(string name, string displayName)
    {
        var status = Create(name, displayName);
        status.IsDefault = true;
        return status;
    }

    #endregion Factory Methods

    #region Update Methods

    public void Update(
        string displayName,
        string? icon,
        string? color,
        int sortOrder,
        bool allowCancel,
        bool allowEdit)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        EnsureNotDeleted();

        DisplayName = displayName.Trim();
        Icon = icon?.Trim();
        Color = color?.Trim();
        SortOrder = sortOrder;
        AllowCancel = allowCancel;
        AllowEdit = allowEdit;
    }

    public void SetAsDefault()
    {
        EnsureNotDeleted();
        IsDefault = true;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
    }

    #endregion Update Methods

    #region Activation & Deletion

    public void Activate()
    {
        if (IsActive) return;
        EnsureNotDeleted();

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        if (IsDefault)
            throw new DomainException("امکان غیرفعال کردن وضعیت پیش‌فرض وجود ندارد.");

        IsActive = false;
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        if (IsDefault)
            throw new DomainException("امکان حذف وضعیت پیش‌فرض وجود ندارد.");

        if (Orders.Any())
            throw new DomainException("امکان حذف وضعیتی که به سفارشات اختصاص داده شده وجود ندارد.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
    }

    #endregion Activation & Deletion

    #region Query Methods

    public bool IsPending() => Name == Statuses.Pending;

    public bool IsProcessing() => Name == Statuses.Processing;

    public bool IsShipped() => Name == Statuses.Shipped;

    public bool IsDelivered() => Name == Statuses.Delivered;

    public bool IsCancelled() => Name == Statuses.Cancelled;

    public bool IsReturned() => Name == Statuses.Returned;

    public bool IsRefunded() => Name == Statuses.Refunded;

    public bool IsFinalStatus() =>
        Name is Statuses.Delivered or Statuses.Cancelled or Statuses.Refunded;

    public bool CanTransitionTo(OrderStatus newStatus)
    {
        if (newStatus == null) return false;

        // قوانین انتقال وضعیت
        return Name switch
        {
            Statuses.Pending => newStatus.Name is Statuses.Processing or Statuses.Cancelled,
            Statuses.Processing => newStatus.Name is Statuses.Shipped or Statuses.Cancelled,
            Statuses.Shipped => newStatus.Name is Statuses.Delivered or Statuses.Returned,
            Statuses.Delivered => newStatus.Name is Statuses.Returned or Statuses.Refunded,
            Statuses.Returned => newStatus.Name is Statuses.Refunded,
            _ => false
        };
    }

    #endregion Query Methods

    #region Private Methods

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("وضعیت حذف شده است.");
    }

    #endregion Private Methods
}