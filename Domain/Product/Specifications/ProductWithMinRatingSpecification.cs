namespace Domain.Product.Specifications;

public class ProductWithMinRatingSpecification : Specification<Product>
{
    private readonly decimal _minRating;

    public ProductWithMinRatingSpecification(decimal minRating)
    {
        _minRating = minRating;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.IsActive && !p.IsDeleted && p.Stats.AverageRating >= _minRating;
    }
}