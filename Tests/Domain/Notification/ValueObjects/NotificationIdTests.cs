using Domain.Notification.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Notification.ValueObjects;

public class NotificationIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuid()
    {
        NotificationId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_ProducesUniqueValuesAcrossCalls()
    {
        NotificationId.NewId().Value.ShouldNotBe(NotificationId.NewId().Value);
    }

    [Fact]
    public void From_WithValidGuid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();

        NotificationId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() => NotificationId.From(Guid.Empty));
        ex.Message.ShouldBe("NotificationId cannot be empty.");
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidString()
    {
        var guid = Guid.NewGuid();

        NotificationId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var sut = NotificationId.From(guid);

        Guid asGuid = sut;

        asGuid.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForSameGuid_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        NotificationId.From(guid).ShouldBe(NotificationId.From(guid));
    }

    [Fact]
    public void Equality_ForDifferentGuid_TreatsInstancesAsNotEqual()
    {
        NotificationId.From(Guid.NewGuid()).ShouldNotBe(NotificationId.From(Guid.NewGuid()));
    }
}
