using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class FilePathTests
{
    [Fact]
    public void Create_WithSimplePath_ParsesFileNameAndExtensionAndDirectory()
    {
        var sut = FilePath.Create("uploads/images/logo.png");

        sut.Value.ShouldBe("uploads/images/logo.png");
        sut.FileName.ShouldBe("logo.png");
        sut.Extension.ShouldBe("png");
        sut.Directory.ShouldBe("uploads/images");
    }

    [Fact]
    public void Create_WithBackslashes_NormalizesToForwardSlashes()
    {
        FilePath.Create(@"uploads\images\logo.png").Value.ShouldBe("uploads/images/logo.png");
    }

    [Fact]
    public void Create_WithLeadingSlash_TrimsLeadingSlash()
    {
        FilePath.Create("/uploads/logo.png").Value.ShouldBe("uploads/logo.png");
    }

    [Fact]
    public void Create_WithUppercaseExtension_LowercasesExtension()
    {
        FilePath.Create("logo.PNG").Extension.ShouldBe("png");
    }

    [Fact]
    public void Create_WithoutExtension_ReturnsEmptyExtension()
    {
        var sut = FilePath.Create("uploads/README");

        sut.Extension.ShouldBe(string.Empty);
        sut.FileName.ShouldBe("README");
    }

    [Fact]
    public void Create_WithTrailingDot_ReturnsEmptyExtension()
    {
        FilePath.Create("uploads/file.").Extension.ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_FileInRoot_ReturnsEmptyDirectory()
    {
        var sut = FilePath.Create("logo.png");

        sut.Directory.ShouldBe(string.Empty);
        sut.FileName.ShouldBe("logo.png");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => FilePath.Create(input));
    }

    [Theory]
    [InlineData("uploads/../etc/passwd")]
    [InlineData("uploads/logo:x.png")]
    [InlineData("uploads/logo*.png")]
    [InlineData("uploads/logo?.png")]
    [InlineData("uploads/logo\".png")]
    [InlineData("uploads/logo<.png")]
    [InlineData("uploads/logo>.png")]
    [InlineData("uploads/logo|.png")]
    public void Create_WithForbiddenCharacter_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => FilePath.Create(input));
    }

    [Fact]
    public void Create_WithPathLongerThan500Chars_ThrowsDomainException()
    {
        var input = new string('a', 501) + ".txt";

        Should.Throw<DomainException>(() => FilePath.Create(input));
    }

    [Fact]
    public void CreateForUpload_WithDirectoryAndFileName_JoinsThem()
    {
        var sut = FilePath.CreateForUpload("uploads/images", "logo.png");

        sut.Value.ShouldBe("uploads/images/logo.png");
    }

    [Fact]
    public void CreateForUpload_WithTrailingSlashOnDirectoryAndLeadingSlashOnFile_JoinsCleanly()
    {
        var sut = FilePath.CreateForUpload("uploads/images/", "/logo.png");

        sut.Value.ShouldBe("uploads/images/logo.png");
    }

    [Theory]
    [InlineData("", "logo.png")]
    [InlineData("   ", "logo.png")]
    [InlineData("uploads", "")]
    [InlineData("uploads", "   ")]
    public void CreateForUpload_WithEmptyPart_ThrowsDomainException(string dir, string fileName)
    {
        Should.Throw<DomainException>(() => FilePath.CreateForUpload(dir, fileName));
    }

    [Theory]
    [InlineData("logo.jpg")]
    [InlineData("logo.jpeg")]
    [InlineData("logo.png")]
    [InlineData("logo.gif")]
    [InlineData("logo.webp")]
    [InlineData("logo.bmp")]
    [InlineData("logo.svg")]
    public void IsImage_ForImageExtensions_ReturnsTrue(string fileName)
    {
        FilePath.Create(fileName).IsImage().ShouldBeTrue();
    }

    [Fact]
    public void IsImage_ForNonImageExtension_ReturnsFalse()
    {
        FilePath.Create("doc.pdf").IsImage().ShouldBeFalse();
    }

    [Theory]
    [InlineData("a.pdf")]
    [InlineData("a.doc")]
    [InlineData("a.docx")]
    [InlineData("a.xls")]
    [InlineData("a.xlsx")]
    [InlineData("a.ppt")]
    [InlineData("a.pptx")]
    [InlineData("a.txt")]
    public void IsDocument_ForDocumentExtensions_ReturnsTrue(string fileName)
    {
        FilePath.Create(fileName).IsDocument().ShouldBeTrue();
    }

    [Fact]
    public void IsDocument_ForImageExtension_ReturnsFalse()
    {
        FilePath.Create("a.png").IsDocument().ShouldBeFalse();
    }

    [Theory]
    [InlineData("a.mp4")]
    [InlineData("a.avi")]
    [InlineData("a.mkv")]
    [InlineData("a.mov")]
    [InlineData("a.wmv")]
    [InlineData("a.flv")]
    public void IsVideo_ForVideoExtensions_ReturnsTrue(string fileName)
    {
        FilePath.Create(fileName).IsVideo().ShouldBeTrue();
    }

    [Theory]
    [InlineData("a.jpg", "image/jpeg")]
    [InlineData("a.jpeg", "image/jpeg")]
    [InlineData("a.png", "image/png")]
    [InlineData("a.pdf", "application/pdf")]
    [InlineData("a.mp4", "video/mp4")]
    [InlineData("a.txt", "text/plain")]
    [InlineData("a.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void GetContentType_ForKnownExtension_ReturnsExpectedMime(string fileName, string expected)
    {
        FilePath.Create(fileName).GetContentType().ShouldBe(expected);
    }

    [Fact]
    public void GetContentType_ForUnknownExtension_ReturnsOctetStream()
    {
        FilePath.Create("a.xyz").GetContentType().ShouldBe("application/octet-stream");
    }

    [Fact]
    public void GetContentType_ForNoExtension_ReturnsOctetStream()
    {
        FilePath.Create("uploads/README").GetContentType().ShouldBe("application/octet-stream");
    }

    [Fact]
    public void WithNewFileName_ReplacesFileNameInSameDirectory()
    {
        var original = FilePath.Create("uploads/images/logo.png");

        var replaced = original.WithNewFileName("banner.jpg");

        replaced.Directory.ShouldBe("uploads/images");
        replaced.FileName.ShouldBe("banner.jpg");
        replaced.Value.ShouldBe("uploads/images/banner.jpg");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithNewFileName_WithEmptyName_ThrowsDomainException(string newName)
    {
        var sut = FilePath.Create("uploads/logo.png");

        Should.Throw<DomainException>(() => sut.WithNewFileName(newName));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        FilePath.Create("uploads/logo.png").ToString().ShouldBe("uploads/logo.png");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = FilePath.Create("uploads/logo.png");

        s.ShouldBe("uploads/logo.png");
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValueDifferentCasing_TreatsInstancesAsEqual()
    {
        FilePath.Create("uploads/Logo.PNG").ShouldBe(FilePath.Create("Uploads/logo.png"));
    }
}
