using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.User.ValueObjects;

public class UserAddressIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        UserAddressId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => UserAddressId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        UserAddressId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        UserAddressId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        UserAddressId.From(guid).ShouldBe(UserAddressId.From(guid));
    }
}
