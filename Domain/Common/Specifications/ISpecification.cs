namespace Domain.Common.Specifications;

public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);

    Expression<Func<T, bool>> ToExpression();
}