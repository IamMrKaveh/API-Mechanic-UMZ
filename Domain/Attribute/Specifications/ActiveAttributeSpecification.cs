using Domain.Attribute.Aggregates;

namespace Domain.Attribute.Specifications;

public class ActiveAttributeSpecification : Specification<AttributeType>
{
    public override Expression<Func<AttributeType, bool>> ToExpression()
    {
        return a => a.IsActive;
    }
}