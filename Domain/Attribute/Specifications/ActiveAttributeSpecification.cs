using System.Linq.Expressions;
using Domain.Attribute.Aggregates;
using Domain.Common.Specifications;

namespace Domain.Attribute.Specifications;

public class ActiveAttributeSpecification : Specification<AttributeType>
{
    public override Expression<Func<AttributeType, bool>> ToExpression()
    {
        return a => a.IsActive;
    }
}