namespace Domain.Categories.Exceptions;

public class CategoryGroupNotFoundException : DomainException
{
    public int GroupId { get; }

    public CategoryGroupNotFoundException(int groupId)
        : base($"گروه با شناسه {groupId} یافت نشد.")
    {
        GroupId = groupId;
    }
}