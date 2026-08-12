using Application.Common.Formatting;
using SharedKernel.Constants;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.Common.Formatting;

public class UserFullNameFormatterTests
{
    [Fact]
    public void Format_WithFirstAndLast_ReturnsCombinedTrimmedName()
    {
        var sut = UserFullNameFormatter.Format("Ali", "Rezaei");

        sut.ShouldBe("Ali Rezaei");
    }

    [Fact]
    public void Format_WithSurroundingWhitespace_TrimsIndividualParts()
    {
        var sut = UserFullNameFormatter.Format("  Ali  ", "  Rezaei  ");

        sut.ShouldBe("Ali Rezaei");
    }

    [Fact]
    public void Format_WithOnlyFirstName_ReturnsFirstNameTrimmed()
    {
        var sut = UserFullNameFormatter.Format("Ali", null);

        sut.ShouldBe("Ali");
    }

    [Fact]
    public void Format_WithOnlyLastName_ReturnsLastNameTrimmed()
    {
        var sut = UserFullNameFormatter.Format(null, "Rezaei");

        sut.ShouldBe("Rezaei");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "")]
    [InlineData("", null)]
    public void Format_WhenBothPartsMissing_ReturnsDeletedUserDisplayName(string? first, string? last)
    {
        var sut = UserFullNameFormatter.Format(first, last);

        sut.ShouldBe(UserConstants.DeletedUserDisplayName);
    }

    [Fact]
    public void Format_WhenUserNull_ReturnsDeletedUserDisplayName()
    {
        Users? user = null;

        var sut = UserFullNameFormatter.Format(user);

        sut.ShouldBe(UserConstants.DeletedUserDisplayName);
    }
}
