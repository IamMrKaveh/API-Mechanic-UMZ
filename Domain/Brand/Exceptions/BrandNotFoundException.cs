namespace Domain.Brand.Exceptions;

public class BrandNotFoundException : DomainException
{
    public int GroupId { get; }

    public BrandNotFoundException(int groupId)
        : base($"گروه با شناسه {groupId} یافت نشد.")
    {
        GroupId = groupId;
    }
}