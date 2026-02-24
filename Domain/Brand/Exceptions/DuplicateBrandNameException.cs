namespace Domain.Brand.Exceptions;

public class DuplicateBrandNameException : DomainException
{
    public string GroupName { get; }
    public int CategoryId { get; }

    public DuplicateBrandNameException(string groupName, int categoryId)
        : base($"گروه با نام '{groupName}' در این دسته‌بندی وجود دارد.")
    {
        GroupName = groupName;
        CategoryId = categoryId;
    }
}