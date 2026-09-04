using Domain.Media.Exceptions;
using SharedKernel.Exceptions;

namespace Tests.Domain.Media.Exceptions;

public class MediaExceptionsTests
{
    [Fact]
    public void InvalidFileTypeException_WithAllowedTypes_ListsThemInMessage()
    {
        var allowed = new[] { "image/jpeg", "image/png" };

        var sut = new InvalidFileTypeException("application/pdf", allowed);

        sut.FileType.ShouldBe("application/pdf");
        sut.AllowedTypes.ShouldBe(allowed);
        sut.ErrorCode.ShouldBe("INVALID_FILE_TYPE");
        sut.Message.ShouldContain("application/pdf");
        sut.Message.ShouldContain("image/jpeg");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidFileTypeException_WithoutAllowedTypes_HasEmptyList()
    {
        var sut = new InvalidFileTypeException("video/mp4");

        sut.FileType.ShouldBe("video/mp4");
        sut.AllowedTypes.ShouldBeEmpty();
        sut.Message.ShouldContain("video/mp4");
    }

    [Fact]
    public void InvalidFileTypeException_AllowedTypes_AreExposedReadOnly()
    {
        var sut = new InvalidFileTypeException("x", new List<string> { "a", "b" });

        sut.AllowedTypes.Count.ShouldBe(2);
    }
}
