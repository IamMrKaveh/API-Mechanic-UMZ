using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Order.ValueObjects;

public class DeliveryAddressTests
{
    [Fact]
    public void Create_WithValidFields_ReturnsAddressWithTrimmedValues()
    {
        var sut = DeliveryAddress.Create("  Tehran  ", "  Tehran  ", "  ValiAsr  ", "  1234567890  ");

        sut.Province.ShouldBe("Tehran");
        sut.City.ShouldBe("Tehran");
        sut.Street.ShouldBe("ValiAsr");
        sut.PostalCode.ShouldBe("1234567890");
    }

    [Theory]
    [InlineData(null, "c", "s", "p")]
    [InlineData("", "c", "s", "p")]
    [InlineData("   ", "c", "s", "p")]
    [InlineData("prov", null, "s", "p")]
    [InlineData("prov", "", "s", "p")]
    [InlineData("prov", "c", null, "p")]
    [InlineData("prov", "c", "", "p")]
    [InlineData("prov", "c", "s", null)]
    [InlineData("prov", "c", "s", "")]
    public void Create_WithAnyNullOrWhitespaceField_ThrowsDomainException(
        string? province, string? city, string? street, string? postalCode)
    {
        Should.Throw<DomainException>(() =>
            DeliveryAddress.Create(province!, city!, street!, postalCode!));
    }

    [Fact]
    public void ToString_ConcatenatesFieldsInReadableFormat()
    {
        var sut = DeliveryAddress.Create("Tehran", "Tehran", "ValiAsr", "1234567890");

        sut.ToString().ShouldBe("Tehran, Tehran, ValiAsr - 1234567890");
    }

    [Fact]
    public void Equality_ForRecordWithSameFields_TreatsInstancesAsEqual()
    {
        var a = DeliveryAddress.Create("Tehran", "Tehran", "ValiAsr", "1234567890");
        var b = DeliveryAddress.Create("Tehran", "Tehran", "ValiAsr", "1234567890");

        a.ShouldBe(b);
    }
}
