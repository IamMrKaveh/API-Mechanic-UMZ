namespace Domain.Categories.Exceptions;

public class DuplicateCategoryGroupNameException : DomainException
{
    public string GroupName { get; }
    public int CategoryId { get; }

    public DuplicateCategoryGroupNameException(string groupName, int categoryId)
        : base($"گروه با نام '{groupName}' در این دسته‌بندی وجود دارد.")
    {
        GroupName = groupName;
        CategoryId = categoryId;
    }
}