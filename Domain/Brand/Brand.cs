namespace Domain.Brand;

public class Brand : BaseEntity, IAuditable, ISoftDeletable, IActivatable
{
    private readonly List<Product.Product> _products = new();

    public BrandName Name { get; private set; } = null!;
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

    // Navigation for EF Core
    public ICollection<Media.Media> Images { get; private set; } = [];

    public Category.Category Category { get; private set; } = null!;
    public IReadOnlyCollection<Product.Product> Products => _products.AsReadOnly();

    public int ActiveProductsCount => _products.Count(p => !p.IsDeleted && p.IsActive);
    public int TotalProductsCount => _products.Count(p => !p.IsDeleted);

    public Brand()
    { }

    internal static Brand Create(Category.Category category, string name, string? description = null)
    {
        Guard.Against.Null(category, nameof(category));

        return new Brand
        {
            Name = BrandName.Create(name),
            Slug = Domain.Category.ValueObjects.Slug.Create(name),
            Description = description?.Trim(),
            CategoryId = category.Id,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SortOrder = 0
        };
    }

    internal void Update(string name, int categoryId, string? description = null)
    {
        EnsureNotDeleted();

        Name = BrandName.Create(name);
        Slug = Domain.Category.ValueObjects.Slug.Create(name);
        Description = description?.Trim();

        if (CategoryId != categoryId)
            CategoryId = categoryId;

        UpdatedAt = DateTime.UtcNow;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        if (sortOrder < 0) throw new DomainException("ترتیب نمایش نمی‌تواند منفی باشد.");
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void MoveTo(Category.Category targetCategory)
    {
        Guard.Against.Null(targetCategory, nameof(targetCategory));
        EnsureNotDeleted();

        if (targetCategory.IsDeleted) throw new DomainException("امکان انتقال به دسته‌بندی حذف‌شده وجود ندارد.");

        CategoryId = targetCategory.Id;
        Category = targetCategory;
        UpdatedAt = DateTime.UtcNow;
    }

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

    internal void Restore(Category.Category parentCategory)
    {
        if (!IsDeleted) return;
        if (parentCategory.IsDeleted) throw new DomainException("امکان بازگردانی گروه در دسته‌بندی حذف‌شده وجود ندارد.");

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasActiveProducts() => _products.Any(p => !p.IsDeleted && p.IsActive);

    public bool HasProducts() => _products.Any(p => !p.IsDeleted);

    public IEnumerable<Product.Product> GetActiveProducts() => _products.Where(p => !p.IsDeleted && p.IsActive);

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("گروه حذف شده است.");
    }
}