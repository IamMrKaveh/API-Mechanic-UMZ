namespace Domain.Category.Exceptions;

public class CategoryNotFoundException : DomainException
{
    public int CategoryId { get; }

    public CategoryNotFoundException(int categoryId)
        : base($"دسته‌بندی با شناسه {categoryId} یافت نشد.")
    {
        CategoryId = categoryId;
    }
}