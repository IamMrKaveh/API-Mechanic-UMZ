namespace Domain.Category.Exceptions;

public class CategoryHasActiveProductsException(int categoryId, int productCount) : DomainException($"امکان حذف دسته‌بندی وجود ندارد. تعداد {productCount} محصول فعال در این دسته‌بندی وجود دارد.")
{
    public int CategoryId { get; } = categoryId;
    public int ProductCount { get; } = productCount;
}