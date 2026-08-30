using Domain.Audit.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Audit.ValueObjects;

public class AuditLogIdTests
{
    [Fact]
    public void NewId_Always_ProducesNonEmptyGuidValue()
    {
        var sut = AuditLogId.NewId();

        sut.ShouldNotBeNull();
        sut.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_CalledTwice_ProducesDistinctInstances()
    {
        var first = AuditLogId.NewId();
        var second = AuditLogId.NewId();

        first.Value.ShouldNotBe(second.Value);
        first.ShouldNotBe(second);
    }

    [Fact]
    public void NewId_ProducesInstanceAssignableToIStronglyTypedId()
    {
        AuditLogId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void From_WithNonEmptyGuid_WrapsGuidWithoutMutation()
    {
        var guid = Guid.NewGuid();

        var sut = AuditLogId.From(guid);

        sut.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => AuditLogId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainExceptionWithExpectedMessage()
    {
        var exception = Should.Throw<DomainException>(() => AuditLogId.From(Guid.Empty));

        exception.Message.ShouldBe("AuditLogId cannot be empty.");
    }

    [Fact]
    public void From_WithSameGuidTwice_ProducesEqualRecords()
    {
        var guid = Guid.NewGuid();

        var a = AuditLogId.From(guid);
        var b = AuditLogId.From(guid);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void From_WithDifferentGuids_ProducesUnequalRecords()
    {
        var a = AuditLogId.From(Guid.NewGuid());
        var b = AuditLogId.From(Guid.NewGuid());

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var sut = AuditLogId.From(guid);

        Guid asGuid = sut;

        asGuid.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();
        var sut = AuditLogId.From(guid);

        sut.ToString().ShouldBe(guid.ToString());
    }
}

