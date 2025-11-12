namespace DataAccessLayer.Models.Base;

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


public interface IBaseEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}