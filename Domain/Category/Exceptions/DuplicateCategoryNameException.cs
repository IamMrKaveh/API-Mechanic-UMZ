using Domain.Category.ValueObjects;

namespace Domain.Category.Exceptions;

public sealed class DuplicateCategoryNameException(CategoryName categoryName) : DomainException($"دسته‌بندی با نام '{categoryName}' قبلاً وجود دارد.")
{
    public CategoryName CategoryName { get; } = categoryName;

    public override string ErrorCode => "DUPLICATE_CATEGORY_NAME";
}