namespace DataAccessLayer.Models.Product.Interfaces;

public interface IProductType : IBaseEntity
{
    public string? Icon { get; set; }
    public ICollection<TProducts>? Products { get; set; }
}