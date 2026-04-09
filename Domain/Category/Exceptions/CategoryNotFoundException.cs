using Domain.Category.ValueObjects;

namespace Domain.Category.Exceptions;

public sealed class CategoryNotFoundException : DomainException
{
    public CategoryId CategoryId { get; }

    public override string ErrorCode => "CATEGORY_NOT_FOUND";

    public CategoryNotFoundException(CategoryId categoryId)
        : base($"دسته‌بندی با شناسه {categoryId} یافت نشد.")
    {
        CategoryId = categoryId;
    }
}