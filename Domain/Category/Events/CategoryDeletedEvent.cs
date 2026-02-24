namespace Domain.Category.Events;

public class CategoryDeletedEvent : DomainEvent
{
    public int CategoryId { get; }
    public int? DeletedBy { get; }

    public CategoryDeletedEvent(int categoryId, int? deletedBy)
    {
        CategoryId = categoryId;
        DeletedBy = deletedBy;
    }
}