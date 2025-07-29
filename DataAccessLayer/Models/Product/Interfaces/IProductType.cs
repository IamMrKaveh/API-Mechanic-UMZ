namespace DataAccessLayer.Models.Product.Interfaces;

public interface IProductType : IBaseEntity
{
    public ICollection<TProducts>? Products { get; set; }
}