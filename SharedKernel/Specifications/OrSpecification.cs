namespace SharedKernel.Specifications;

public class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    private readonly Specification<T> _left = left;
    private readonly Specification<T> _right = right;

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ParameterReplacer(leftExpression.Parameters[0], parameter);
        var rightVisitor = new ParameterReplacer(rightExpression.Parameters[0], parameter);

        var left = leftVisitor.Visit(leftExpression.Body);
        var right = rightVisitor.Visit(rightExpression.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left!, right!), parameter);
    }
}