namespace Domain.Categories;

/// <summary>
/// Aggregate Root - تمام دسترسی به CategoryGroup فقط از طریق این موجودیت انجام می‌شود.
/// </summary>
public class Category : AggregateRoot, IAuditable, ISoftDeletable, IActivatable
{
    public CategoryName Name { get; private set; } = null!;
    public Slug? Slug { get; private set; }
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Concurrency
    public new byte[]? RowVersion { get; private set; }

    //Navigation for EF Core
    public ICollection<Media.Media> Images { get; private set; } = [];

    private readonly List<CategoryGroup> _categoryGroups = [];
    public IReadOnlyCollection<CategoryGroup> CategoryGroups => _categoryGroups.AsReadOnly();

    // Computed
    public int ActiveGroupsCount => _categoryGroups.Count(g => !g.IsDeleted && g.IsActive);

    public int TotalProductsCount => _categoryGroups
        .Where(g => !g.IsDeleted)
        .SelectMany(g => g.Products)
        .Count(p => !p.IsDeleted);

    private Category()
    { }

    // ============================================================
    // Factory Method
    // ============================================================

    public static Category Create(string name, string? description = null, int sortOrder = 0)
    {
        var categoryName = CategoryName.Create(name);
        var slug = ValueObjects.Slug.Create(name);

        var category = new Category
        {
            Name = categoryName,
            Slug = slug,
            Description = description?.Trim(),
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        category.AddDomainEvent(new CategoryCreatedEvent(category.Id, categoryName.Value));
        return category;
    }

    // ============================================================
    // Update Methods
    // ============================================================

    public void Update(string name, string? description, int sortOrder)
    {
        EnsureNotDeleted();

        var newName = CategoryName.Create(name);
        var oldName = Name.Value;

        Name = newName;
        Slug = ValueObjects.Slug.Create(name);
        Description = description?.Trim();
        SetSortOrder(sortOrder);
        UpdatedAt = DateTime.UtcNow;

        if (!oldName.Equals(newName.Value, StringComparison.OrdinalIgnoreCase))
        {
            AddDomainEvent(new CategoryUpdatedEvent(Id, newName.Value));
        }
    }

    public void UpdateSlug(string slug)
    {
        EnsureNotDeleted();
        Slug = ValueObjects.Slug.FromString(slug);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        EnsureNotDeleted();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new DomainException("ترتیب نمایش نمی‌تواند منفی باشد.");

        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    // ============================================================
    // Group Management - تنها نقطه دسترسی به CategoryGroup
    // ============================================================

    /// <summary>
    /// افزودن گروه جدید - یکتایی نام در محدوده این Category بررسی می‌شود
    /// </summary>
    public CategoryGroup AddGroup(string name, string? description = null)
    {
        EnsureNotDeleted();
        EnsureActive();
        ValidateGroupNameUniqueness(name);

        var group = CategoryGroup.Create(this, name, description);
        _categoryGroups.Add(group);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CategoryGroupCreatedEvent(group.Id, Id, name));
        return group;
    }

    /// <summary>
    /// تغییر نام گروه - یکتایی نام در محدوده این Category بررسی می‌شود
    /// </summary>
    public void RenameGroup(int groupId, string newName, string? description = null)
    {
        EnsureNotDeleted();

        var group = GetGroupOrThrow(groupId);
        ValidateGroupNameUniqueness(newName, groupId);

        group.Update(newName, Id, description);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// حذف نرم گروه - بررسی می‌کند که محصول فعالی ندارد
    /// </summary>
    public void RemoveGroup(int groupId, int? deletedBy = null)
    {
        var group = GetGroupOrThrow(groupId);

        if (group.HasActiveProducts())
            throw new DomainException("امکان حذف گروه دارای محصولات فعال وجود ندارد.");

        group.Delete(deletedBy);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CategoryGroupDeletedEvent(groupId, Id));
    }

    /// <summary>
    /// جدا کردن گروه برای انتقال - فقط توسط CategoryDomainService فراخوانی شود
    /// </summary>
    internal CategoryGroup DetachGroup(int groupId)
    {
        EnsureNotDeleted();

        var group = GetGroupOrThrow(groupId);
        _categoryGroups.Remove(group);
        UpdatedAt = DateTime.UtcNow;

        return group;
    }

    /// <summary>
    /// پذیرش گروه منتقل‌شده - فقط توسط CategoryDomainService فراخوانی شود
    /// </summary>
    internal void AcceptGroup(CategoryGroup group)
    {
        EnsureNotDeleted();
        EnsureActive();

        // بررسی یکتایی نام در Category مقصد
        if (ContainsGroupWithName(group.Name))
            throw new DuplicateCategoryGroupNameException(group.Name, Id);

        group.MoveTo(this);
        _categoryGroups.Add(group);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تغییر ترتیب نمایش گروه‌ها
    /// </summary>
    public void ReorderGroups(IReadOnlyList<int> orderedGroupIds)
    {
        EnsureNotDeleted();

        var activeGroups = _categoryGroups.Where(g => !g.IsDeleted).ToList();

        // بررسی اینکه تمام شناسه‌ها معتبر هستند
        var activeGroupIds = activeGroups.Select(g => g.Id).ToHashSet();
        var providedIds = orderedGroupIds.ToHashSet();

        if (!activeGroupIds.SetEquals(providedIds))
            throw new DomainException("لیست شناسه‌های گروه‌ها با گروه‌های فعال مطابقت ندارد.");

        for (int i = 0; i < orderedGroupIds.Count; i++)
        {
            var group = activeGroups.First(g => g.Id == orderedGroupIds[i]);
            group.UpdateSortOrder(i);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void ActivateGroup(int groupId)
    {
        EnsureNotDeleted();
        EnsureActive();

        var group = GetGroupOrThrow(groupId);
        group.Activate();
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeactivateGroup(int groupId)
    {
        var group = GetGroupOrThrow(groupId);
        group.Deactivate();
        UpdatedAt = DateTime.UtcNow;
    }

    // ============================================================
    // Activation & Deletion
    // ============================================================

    public void Activate()
    {
        if (IsActive) return;
        EnsureNotDeleted();

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CategoryActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        // غیرفعال کردن تمام گروه‌ها
        foreach (var group in _categoryGroups.Where(g => !g.IsDeleted && g.IsActive))
        {
            group.Deactivate();
        }

        AddDomainEvent(new CategoryDeactivatedEvent(Id));
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;
        EnsureCanBeDeleted();

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;

        foreach (var group in _categoryGroups.Where(g => !g.IsDeleted))
        {
            group.Delete(deletedBy);
        }

        AddDomainEvent(new CategoryDeletedEvent(Id, deletedBy));
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

    // ============================================================
    // Query Methods
    // ============================================================

    public CategoryGroup? GetGroup(int groupId)
    {
        return _categoryGroups.FirstOrDefault(g => g.Id == groupId && !g.IsDeleted);
    }

    public IEnumerable<CategoryGroup> GetActiveGroups()
    {
        return _categoryGroups
            .Where(g => !g.IsDeleted && g.IsActive)
            .OrderBy(g => g.SortOrder);
    }

    public bool HasGroups()
    {
        return _categoryGroups.Any(g => !g.IsDeleted);
    }

    public bool HasActiveProducts()
    {
        return _categoryGroups.Any(g => !g.IsDeleted && g.HasActiveProducts());
    }

    public bool ContainsGroupWithName(string name, int? excludeGroupId = null)
    {
        return _categoryGroups.Any(g =>
            !g.IsDeleted &&
            (excludeGroupId == null || g.Id != excludeGroupId) &&
            g.Name.IsSameAs(name));
    }

    // ============================================================
    // Private Methods
    // ============================================================

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("دسته‌بندی حذف شده است.");
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("دسته‌بندی غیرفعال است.");
    }

    private void EnsureCanBeDeleted()
    {
        if (_categoryGroups.Any(g => !g.IsDeleted && g.HasActiveProducts()))
        {
            var productCount = TotalProductsCount;
            throw new CategoryHasActiveProductsException(Id, productCount);
        }
    }

    private CategoryGroup GetGroupOrThrow(int groupId)
    {
        var group = _categoryGroups.FirstOrDefault(g => g.Id == groupId && !g.IsDeleted);
        if (group == null)
            throw new CategoryGroupNotFoundException(groupId);
        return group;
    }

    private void ValidateGroupNameUniqueness(string name, int? excludeGroupId = null)
    {
        if (ContainsGroupWithName(name, excludeGroupId))
        {
            throw new DuplicateCategoryGroupNameException(name.Trim(), Id);
        }
    }
}