namespace Domain.Category.Exceptions;

public class CategoryHasActiveProductsException : DomainException
{
    public int CategoryId { get; }
    public int ProductCount { get; }

    public CategoryHasActiveProductsException(int categoryId, int productCount)
        : base($"امکان حذف دسته‌بندی وجود ندارد. تعداد {productCount} محصول فعال در این دسته‌بندی وجود دارد.")
    {
        CategoryId = categoryId;
        ProductCount = productCount;
    }
}