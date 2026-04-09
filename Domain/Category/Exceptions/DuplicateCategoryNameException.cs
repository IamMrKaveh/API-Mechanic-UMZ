using Domain.Category.ValueObjects;

namespace Domain.Category.Exceptions;

public sealed class DuplicateCategoryNameException : DomainException
{
    public CategoryName CategoryName { get; }

    public override string ErrorCode => "DUPLICATE_CATEGORY_NAME";

    public DuplicateCategoryNameException(CategoryName categoryName)
        : base($"دسته‌بندی با نام '{categoryName}' قبلاً وجود دارد.")
    {
        CategoryName = categoryName;
    }
}