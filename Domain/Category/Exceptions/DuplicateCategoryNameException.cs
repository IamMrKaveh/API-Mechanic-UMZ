namespace Domain.Category.Exceptions;

public class DuplicateCategoryNameException : DomainException
{
    public string CategoryName { get; }

    public DuplicateCategoryNameException(string categoryName)
        : base($"دسته‌بندی با نام '{categoryName}' قبلاً وجود دارد.")
    {
        CategoryName = categoryName;
    }
}