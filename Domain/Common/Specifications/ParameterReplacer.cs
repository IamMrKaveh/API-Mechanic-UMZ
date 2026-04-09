namespace Domain.Common.Specifications;

internal sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
{
    private readonly ParameterExpression _source = source;
    private readonly ParameterExpression _target = target;

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _source ? _target : base.VisitParameter(node);
}