namespace SharedKernel.Specifications;

public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);

    Expression<Func<T, bool>> ToExpression();
}