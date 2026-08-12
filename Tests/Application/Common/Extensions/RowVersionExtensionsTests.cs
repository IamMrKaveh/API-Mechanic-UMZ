using Application.Common.Extensions;

namespace Tests.Application.Common.Extensions;

public class RowVersionExtensionsTests
{
    [Fact]
    public void ToBase64_WhenNull_ReturnsNull()
    {
        byte[]? rowVersion = null;

        var sut = rowVersion.ToBase64();

        sut.ShouldBeNull();
    }

    [Fact]
    public void ToBase64_WhenEmpty_ReturnsEmptyString()
    {
        var rowVersion = Array.Empty<byte>();

        var sut = rowVersion.ToBase64();

        sut.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToBase64_WithBytes_ReturnsBase64EncodedString()
    {
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var sut = rowVersion.ToBase64();

        sut.ShouldBe(Convert.ToBase64String(rowVersion));
    }

    [Fact]
    public void FromBase64RowVersion_WhenNull_ReturnsNull()
    {
        string? base64 = null;

        var sut = base64.FromBase64RowVersion();

        sut.ShouldBeNull();
    }

    [Fact]
    public void FromBase64RowVersion_WhenEmpty_ReturnsNull()
    {
        var sut = string.Empty.FromBase64RowVersion();

        sut.ShouldBeNull();
    }

    [Fact]
    public void FromBase64RowVersion_WhenWhitespace_ReturnsNull()
    {
        var sut = "   ".FromBase64RowVersion();

        sut.ShouldBeNull();
    }

    [Fact]
    public void FromBase64RowVersion_WithValidBase64_ReturnsDecodedBytes()
    {
        var original = new byte[] { 10, 20, 30, 40 };
        var base64 = Convert.ToBase64String(original);

        var sut = base64.FromBase64RowVersion();

        sut.ShouldBe(original);
    }

    [Fact]
    public void Roundtrip_EncodeThenDecode_ReturnsOriginalBytes()
    {
        var original = new byte[] { 0, 1, 2, 3, 255, 128, 64 };

        var roundtripped = original.ToBase64().FromBase64RowVersion();

        roundtripped.ShouldBe(original);
    }
}
