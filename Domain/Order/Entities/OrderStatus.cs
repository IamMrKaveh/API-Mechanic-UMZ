using Domain.Common.Abstractions;
using Domain.Order.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Entities;

public class OrderStatus : AggregateRoot<OrderStatusId>, IActivatable
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
    public byte[] RowVersion { get; private set; } = [];

    private OrderStatus()
    { }

    private OrderStatus(
        OrderStatusId id,
        string name,
        string displayName,
        string? icon = null,
        string? color = null,
        int sortOrder = 0,
        bool allowCancel = false,
        bool allowEdit = false) : base(id)
    {
        Name = name.Trim();
        DisplayName = displayName.Trim();
        Icon = icon?.Trim();
        Color = color?.Trim();
        SortOrder = sortOrder;
        AllowCancel = allowCancel;
        AllowEdit = allowEdit;
        IsActive = true;
        IsDefault = false;
    }

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

        var status = new OrderStatus(
            OrderStatusId.NewId(),
            name.Trim(),
            displayName.Trim(),
            icon?.Trim(),
            color?.Trim(),
            sortOrder,
            allowCancel,
            allowEdit);

        status.RaiseDomainEvent(new OrderStatusCreatedDomainEvent(
            status.Id,
            status.Name,
            status.DisplayName,
            status.SortOrder));

        return status;
    }

    public void Update(
        string displayName,
        string? icon,
        string? color,
        int sortOrder,
        bool allowCancel,
        bool allowEdit)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));

        DisplayName = displayName.Trim();
        Icon = icon?.Trim();
        Color = color?.Trim();
        SortOrder = sortOrder;
        AllowCancel = allowCancel;
        AllowEdit = allowEdit;

        RaiseDomainEvent(new OrderStatusUpdatedDomainEvent(
            Id,
            Name,
            DisplayName,
            SortOrder));
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        RaiseDomainEvent(new OrderStatusActivationChangedDomainEvent(Id, Name, true));
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        if (IsDefault)
            throw new DomainException("امکان غیرفعال کردن وضعیت پیش‌فرض وجود ندارد.");
        IsActive = false;
        RaiseDomainEvent(new OrderStatusActivationChangedDomainEvent(Id, Name, false));
    }

    public void SetAsDefault()
    {
        if (!IsActive)
            throw new DomainException("وضعیت غیرفعال نمی‌تواند به‌عنوان پیش‌فرض تنظیم شود.");
        if (IsDefault) return;
        IsDefault = true;
        RaiseDomainEvent(new OrderStatusDefaultChangedDomainEvent(Id, Name, true));
    }

    public void UnsetAsDefault()
    {
        if (!IsDefault) return;
        IsDefault = false;
        RaiseDomainEvent(new OrderStatusDefaultChangedDomainEvent(Id, Name, false));
    }

    public void MarkAsDeleted()
    {
        RaiseDomainEvent(new OrderStatusDeletedDomainEvent(Id, Name));
    }
}