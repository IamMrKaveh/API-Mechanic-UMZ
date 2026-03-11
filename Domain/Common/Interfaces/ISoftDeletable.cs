namespace Domain.Common.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    int? DeletedBy { get; }
}