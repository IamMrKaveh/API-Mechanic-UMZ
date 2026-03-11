namespace Domain.Category.Exceptions;

public class CategoryNotFoundException(int categoryId) : DomainException($"دسته‌بندی با شناسه {categoryId} یافت نشد.")
{
    public int CategoryId { get; } = categoryId;
}