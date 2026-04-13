namespace SharedKernel.Abstractions.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    Guid? DeletedBy { get; }
}