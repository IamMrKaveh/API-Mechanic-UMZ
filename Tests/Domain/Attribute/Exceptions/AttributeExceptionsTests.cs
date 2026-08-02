using Domain.Attribute.Exceptions;
using Domain.Attribute.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Attribute.Exceptions;

public class AttributeExceptionsTests
{
    [Fact]
    public void AttributeTypeNotFoundException_ExposesIdAndErrorCodeAndMessage()
    {
        var id = AttributeTypeId.NewId();

        var sut = new AttributeTypeNotFoundException(id);

        sut.AttributeTypeId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("ATTRIBUTE_TYPE_NOT_FOUND");
        sut.Message.ShouldContain(id.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void AttributeValueNotFoundException_ExposesIdAndErrorCodeAndMessage()
    {
        var id = AttributeValueId.NewId();

        var sut = new AttributeValueNotFoundException(id);

        sut.AttributeValueId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("ATTRIBUTE_VALUE_NOT_FOUND");
        sut.Message.ShouldContain(id.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void DuplicateAttributeException_ExposesNameAndErrorCodeAndMessage()
    {
        var sut = new DuplicateAttributeException("Color");

        sut.Name.ShouldBe("Color");
        sut.ErrorCode.ShouldBe("DUPLICATE_ATTRIBUTE");
        sut.Message.ShouldContain("Color");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void DuplicateAttributeValueException_ExposesNameAndErrorCodeAndMessage()
    {
        var sut = new DuplicateAttributeValueException("Red");

        sut.Name.ShouldBe("Red");
        sut.ErrorCode.ShouldBe("DUPLICATE_ATTRIBUTE_VALUE");
        sut.Message.ShouldContain("Red");
        sut.ShouldBeAssignableTo<DomainException>();
    }
}
