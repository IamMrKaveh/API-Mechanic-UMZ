namespace Domain.Common.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    private Func<T, bool>? _compiledPredicate;
    private readonly Lock _syncLock = new();

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        if (_compiledPredicate is null)
        {
            lock (_syncLock)
            {
                _compiledPredicate ??= ToExpression().Compile();
            }
        }

        return _compiledPredicate(entity);
    }

    public Specification<T> And(Specification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }

    public Specification<T> Or(Specification<T> other)
    {
        return new OrSpecification<T>(this, other);
    }

    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}