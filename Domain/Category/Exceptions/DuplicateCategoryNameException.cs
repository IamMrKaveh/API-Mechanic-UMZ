using Domain.Category.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Category.Exceptions;

public sealed class DuplicateCategoryNameException(CategoryName categoryName)
    : AlreadyExistsException<CategoryName>(
        "DUPLICATE_CATEGORY_NAME",
        categoryName,
        $"دسته‌بندی با نام '{categoryName}' قبلاً وجود دارد.")
{
    public CategoryName CategoryName => Value;
}
