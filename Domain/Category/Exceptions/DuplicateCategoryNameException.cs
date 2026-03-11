namespace Domain.Category.Exceptions;

public class DuplicateCategoryNameException(string categoryName) : DomainException($"دسته‌بندی با نام '{categoryName}' قبلاً وجود دارد.")
{
    public string CategoryName { get; } = categoryName;
}