using Domain.User.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace Tests.Domain.User.ValueObjects;

public class FullNameTests
{
    [Fact]
    public void Create_WithValidPersianNames_ReturnsFullName()
    {
        var sut = FullName.Create("علی", "رضایی");

        sut.FirstName.ShouldBe("علی");
        sut.LastName.ShouldBe("رضایی");
    }

    [Fact]
    public void Create_WithValidEnglishNames_ReturnsFullName()
    {
        var sut = FullName.Create("John", "Doe");

        sut.FirstName.ShouldBe("John");
        sut.LastName.ShouldBe("Doe");
    }

    [Fact]
    public void Create_DoesNotTrimWhitespace()
    {
        var sut = FullName.Create("  John  ", "  Doe  ");

        sut.FirstName.ShouldBe("  John  ");
        sut.LastName.ShouldBe("  Doe  ");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "Doe")]
    [InlineData("John", "")]
    public void Create_WithNullOrWhitespaceNames_AllowsAndStoresEmpty(string? first, string? last)
    {
        var sut = FullName.Create(first, last);

        sut.FirstName.ShouldNotBeNull();
        sut.LastName.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithNameLongerThan50Characters_ThrowsDomainException()
    {
        var longName = new string('a', 51);

        Should.Throw<DomainException>(() => FullName.Create(longName, "Doe"));
    }

    [Fact]
    public void Create_WithNameAt50Characters_Succeeds()
    {
        var boundary = new string('a', 50);

        FullName.Create(boundary, "Doe").FirstName.Length.ShouldBe(50);
    }

    [Theory]
    [InlineData("John123")]
    [InlineData("John-Doe")]
    [InlineData("O'Brien")]
    [InlineData("John.Doe")]
    [InlineData("John_Doe")]
    public void Create_WithNameContainingForbiddenCharacters_ThrowsDomainException(string name)
    {
        Should.Throw<DomainException>(() => FullName.Create(name, "Valid"));
    }

    [Fact]
    public void Empty_ReturnsFullNameWithBothNamesEmpty()
    {
        var sut = FullName.Empty();

        sut.FirstName.ShouldBe(string.Empty);
        sut.LastName.ShouldBe(string.Empty);
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        FullName.Create("John", "Doe").ShouldBe(FullName.Create("JOHN", "doe"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        FullName.Create("John", "Doe").ShouldNotBe(FullName.Create("John", "Smith"));
    }

    [Fact]
    public void GetHashCode_IsCaseInsensitive()
    {
        FullName.Create("John", "Doe").GetHashCode()
            .ShouldBe(FullName.Create("john", "doe").GetHashCode());
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        FullName.Create("John", "Doe").ShouldBeAssignableTo<ValueObject>();
    }
}
