namespace Domain.Base;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    int? DeletedBy { get; set; }
}

public interface IActivatable
{
    bool IsActive { get; set; }
}

public interface IBaseEntity
{
    int Id { get; set; }
}

public abstract class BaseEntity : IBaseEntity, IAuditable, ISoftDeletable, IActivatable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public byte[]? RowVersion { get; set; }
    public bool IsActive { get; set; } = true;
}