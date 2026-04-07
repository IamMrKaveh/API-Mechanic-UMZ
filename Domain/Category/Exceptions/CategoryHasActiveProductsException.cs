using Domain.Common.Exceptions;
using Domain.Category.ValueObjects;

namespace Domain.Category.Exceptions;

public sealed class CategoryHasActiveProductsException : DomainException
{
    public CategoryId CategoryId { get; }
    public int ProductCount { get; }

    public override string ErrorCode => "CATEGORY_HAS_ACTIVE_PRODUCTS";

    public CategoryHasActiveProductsException(CategoryId categoryId, int productCount)
        : base($"امک��ن حذف دسته‌بندی با شناسه {categoryId} وجود ندارد. تعداد {productCount} محصول فعال در این دسته‌بندی وجود دارد.")
    {
        CategoryId = categoryId;
        ProductCount = productCount;
    }
}