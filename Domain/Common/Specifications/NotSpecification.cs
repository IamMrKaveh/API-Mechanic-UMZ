using System;
using System.Linq.Expressions;

namespace Domain.Common.Specifications;

public class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    private readonly Specification<T> _inner = inner;

    public override Expression<Func<T, bool>> ToExpression()
    {
        var innerExpression = _inner.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var visitor = new ParameterReplacer(innerExpression.Parameters[0], parameter);

        var inner = visitor.Visit(innerExpression.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.Not(inner!), parameter);
    }
}