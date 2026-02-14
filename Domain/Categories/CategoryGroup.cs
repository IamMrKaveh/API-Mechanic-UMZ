namespace Domain.Categories;

/// <summary>
/// موجودیت فرزند Category - دسترسی مستقیم از بیرون Aggregate ممنوع است.
/// مدیریت فقط از طریق Category انجام می‌شود.
/// ارتباط با Product یک‌طرفه است (Product به CategoryGroup رجوع می‌کند).
/// </summary>
public class CategoryGroup : BaseEntity, IAuditable, ISoftDeletable, IActivatable
{
    private readonly List<Product.Product> _products = new();

    public CategoryName Name { get; private set; } = null!;
    public Slug? Slug { get; private set; }
    public string? Description { get; private set; }
    public int CategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Audit & Soft Delete
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Concurrency
    public new byte[]? RowVersion { get; private set; }

    // Navigation for EF Core
    public ICollection<Media.Media> Images { get; private set; } = [];

    public Category Category { get; private set; } = null!;
    public IReadOnlyCollection<Product.Product> Products => _products.AsReadOnly();

    // Computed
    public int ActiveProductsCount => _products.Count(p => !p.IsDeleted && p.IsActive);

    public int TotalProductsCount => _products.Count(p => !p.IsDeleted);

    private CategoryGroup()
    { }

    // ============================================================
    // Factory Method - فقط internal (از طریق Category.AddGroup)
    // ============================================================

    internal static CategoryGroup Create(Category category, string name, string? description = null)
    {
        Guard.Against.Null(category, nameof(category));

        var groupName = CategoryName.Create(name);
        var slug = ValueObjects.Slug.Create(name);

        return new CategoryGroup
        {
            Name = groupName,
            Slug = slug,
            Description = description?.Trim(),
            CategoryId = category.Id,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SortOrder = 0
        };
    }

    // ============================================================
    // Update Methods - internal (فقط از طریق Category)
    // ============================================================

    internal void Update(string name, int categoryId, string? description = null)
    {
        EnsureNotDeleted();

        var newName = CategoryName.Create(name);
        Name = newName;
        Slug = ValueObjects.Slug.Create(name);
        Description = description?.Trim();

        if (CategoryId != categoryId)
            CategoryId = categoryId;

        UpdatedAt = DateTime.UtcNow;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new DomainException("ترتیب نمایش نمی‌تواند منفی باشد.");

        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void MoveTo(Category targetCategory)
    {
        Guard.Against.Null(targetCategory, nameof(targetCategory));
        EnsureNotDeleted();

        if (targetCategory.IsDeleted)
            throw new DomainException("امکان انتقال به دسته‌بندی حذف‌شده وجود ندارد.");

        CategoryId = targetCategory.Id;
        Category = targetCategory;
        UpdatedAt = DateTime.UtcNow;
    }

    // ============================================================
    // Activation & Deletion - internal
    // ============================================================

    internal void Activate()
    {
        if (IsActive) return;
        EnsureNotDeleted();

        if (Category != null && !Category.IsActive)
            throw new DomainException("امکان فعال‌سازی گروه در دسته‌بندی غیرفعال وجود ندارد.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        if (HasActiveProducts())
            throw new DomainException("امکان حذف گروه دارای محصولات فعال وجود ندارد.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }

    internal void Restore(Category parentCategory)
    {
        if (!IsDeleted) return;

        if (parentCategory.IsDeleted)
            throw new DomainException("امکان بازگردانی گروه در دسته‌بندی حذف‌شده وجود ندارد.");

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ============================================================
    // Query Methods
    // ============================================================

    public bool HasActiveProducts()
    {
        return _products.Any(p => !p.IsDeleted && p.IsActive);
    }

    public bool HasProducts()
    {
        return _products.Any(p => !p.IsDeleted);
    }

    public IEnumerable<Product.Product> GetActiveProducts()
    {
        return _products.Where(p => !p.IsDeleted && p.IsActive);
    }

    // ============================================================
    // Private Methods
    // ============================================================

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("گروه حذف شده است.");
    }
}