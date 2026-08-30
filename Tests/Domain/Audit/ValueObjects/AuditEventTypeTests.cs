using Domain.Audit.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Audit.ValueObjects;

public class AuditEventTypeTests
{
    [Fact]
    public void StaticInstances_HaveExpectedValues()
    {
        AuditEventType.Authentication.Value.ShouldBe("Authentication");
        AuditEventType.Security.Value.ShouldBe("SecurityEvent");
        AuditEventType.Order.Value.ShouldBe("OrderEvent");
        AuditEventType.Payment.Value.ShouldBe("PaymentEvent");
        AuditEventType.Inventory.Value.ShouldBe("InventoryEvent");
        AuditEventType.Product.Value.ShouldBe("ProductEvent");
        AuditEventType.AdminAction.Value.ShouldBe("AdminEvent");
        AuditEventType.System.Value.ShouldBe("SystemEvent");
        AuditEventType.Error.Value.ShouldBe("Error");
        AuditEventType.Warning.Value.ShouldBe("Warning");
        AuditEventType.Information.Value.ShouldBe("Information");
        AuditEventType.Debug.Value.ShouldBe("Debug");
    }

    [Fact]
    public void StaticInstances_AreDistinctFromEachOther()
    {
        AuditEventType.Authentication.ShouldNotBe(AuditEventType.Security);
        AuditEventType.Order.ShouldNotBe(AuditEventType.Payment);
        AuditEventType.Error.ShouldNotBe(AuditEventType.Warning);
        AuditEventType.Information.ShouldNotBe(AuditEventType.Debug);
    }

    [Theory]
    [InlineData("Authentication")]
    [InlineData("SecurityEvent")]
    [InlineData("Custom.Event.Name")]
    [InlineData("Any-Non-Empty-Value")]
    public void From_WithNonEmptyValue_ReturnsInstanceWithThatValue(string value)
    {
        var sut = AuditEventType.From(value);

        sut.Value.ShouldBe(value);
    }

    [Fact]
    public void From_TrimsSurroundingWhitespaceFromValue()
    {
        var sut = AuditEventType.From("  PaymentEvent  ");

        sut.Value.ShouldBe("PaymentEvent");
    }

    [Fact]
    public void From_TrimsTabsAndNewlinesFromValue()
    {
        var sut = AuditEventType.From("\tOrderEvent\n");

        sut.Value.ShouldBe("OrderEvent");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void From_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => AuditEventType.From(value!));
    }

    [Fact]
    public void From_WithEmptyValue_ThrowsDomainExceptionWithExpectedMessage()
    {
        var exception = Should.Throw<DomainException>(() => AuditEventType.From(""));

        exception.Message.ShouldBe("AuditEventType cannot be empty.");
    }

    [Fact]
    public void From_WithMatchingStaticValue_ProducesEqualRecord()
    {
        var sut = AuditEventType.From("PaymentEvent");

        sut.ShouldBe(AuditEventType.Payment);
        (sut == AuditEventType.Payment).ShouldBeTrue();
    }

    [Fact]
    public void From_WithSameValueTwice_ProducesEqualRecords()
    {
        var a = AuditEventType.From("CustomEvent");
        var b = AuditEventType.From("CustomEvent");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void From_WithDifferentValues_ProducesUnequalRecords()
    {
        var a = AuditEventType.From("EventA");
        var b = AuditEventType.From("EventB");

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void From_IsCaseSensitive()
    {
        var lower = AuditEventType.From("paymentevent");

        lower.ShouldNotBe(AuditEventType.Payment);
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsUnderlyingValue()
    {
        string s = AuditEventType.Order;

        s.ShouldBe("OrderEvent");
    }

    [Fact]
    public void ImplicitOperatorString_UsedInInterpolation_YieldsValue()
    {
        var interpolated = $"{AuditEventType.Security}";

        interpolated.ShouldBe("SecurityEvent");
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        AuditEventType.Debug.ToString().ShouldBe("Debug");
        AuditEventType.Information.ToString().ShouldBe("Information");
    }
}

