namespace Domain.Common.Base;

/// <summary>
/// کلاس پایه برای تمام موجودیت‌ها
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public byte[]? RowVersion { get; protected set; }
}