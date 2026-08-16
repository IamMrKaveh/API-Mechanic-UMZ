using Infrastructure.Storage.Services;

namespace Tests.Infrastructure.Storage.Services;

public class FileMagicBytesValidatorTests
{
    private readonly FileMagicBytesValidator _sut = new();

    [Fact]
    public async Task IsAllowedAsync_WhenStreamIsNull_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.IsAllowedAsync(null!, "image/png"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsAllowedAsync_WhenDeclaredContentTypeIsBlank_ReturnsFalse(string declared)
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var allowed = await _sut.IsAllowedAsync(stream, declared);

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenDeclaredContentTypeIsNull_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var allowed = await _sut.IsAllowedAsync(stream, null!);

        allowed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("image/bmp")]
    [InlineData("image/tiff")]
    public async Task IsAllowedAsync_WhenContentTypeIsNotWhitelisted_ReturnsFalse(string declared)
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var allowed = await _sut.IsAllowedAsync(stream, declared);

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenStreamIsNotSeekable_ReturnsFalse()
    {
        using var nonSeekable = new NonSeekableStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var allowed = await _sut.IsAllowedAsync(nonSeekable, "image/png");

        allowed.ShouldBeFalse();
    }

    [Theory]
    [InlineData((byte)0xDB)]
    [InlineData((byte)0xE0)]
    [InlineData((byte)0xE1)]
    [InlineData((byte)0xEE)]
    public async Task IsAllowedAsync_ForJpegDeclaredWithMatchingSignature_ReturnsTrue(byte fourthByte)
    {
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, fourthByte });

        var allowed = await _sut.IsAllowedAsync(stream, "image/jpeg");

        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ForJpegDeclaredWithForeignSignature_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var allowed = await _sut.IsAllowedAsync(stream, "image/jpeg");

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_ForPngDeclaredWithMatchingSignature_ReturnsTrue()
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D });

        var allowed = await _sut.IsAllowedAsync(stream, "image/png");

        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ForPngDeclaredWithForeignSignature_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00, 0x00, 0x00 });

        var allowed = await _sut.IsAllowedAsync(stream, "image/png");

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_ForGif87aDeclaredWithMatchingSignature_ReturnsTrue()
    {
        using var stream = new MemoryStream(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x00, 0x00 });

        var allowed = await _sut.IsAllowedAsync(stream, "image/gif");

        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ForGif89aDeclaredWithMatchingSignature_ReturnsTrue()
    {
        using var stream = new MemoryStream(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00 });

        var allowed = await _sut.IsAllowedAsync(stream, "image/gif");

        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ForGifDeclaredWithForeignSignature_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var allowed = await _sut.IsAllowedAsync(stream, "image/gif");

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_ForWebPDeclaredWithMatchingRiffAndWebpMarker_ReturnsTrue()
    {
        using var stream = new MemoryStream(new byte[]
        {
        0x52, 0x49, 0x46, 0x46,
        0x00, 0x00, 0x00, 0x00,
        0x57, 0x45, 0x42, 0x50
        });

        var allowed = await _sut.IsAllowedAsync(stream, "image/webp");

        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ForWebPDeclaredWithRiffButNonWebpContainer_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[]
        {
        0x52, 0x49, 0x46, 0x46,
        0x00, 0x00, 0x00, 0x00,
        0x57, 0x41, 0x56, 0x45
        });

        var allowed = await _sut.IsAllowedAsync(stream, "image/webp");

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_ForWebPDeclaredWithRiffButHeaderShorterThan12Bytes_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[] { 0x52, 0x49, 0x46, 0x46 });

        var allowed = await _sut.IsAllowedAsync(stream, "image/webp");

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenStreamShorterThanMinimumSignature_ReturnsFalse()
    {
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });

        var allowed = await _sut.IsAllowedAsync(stream, "image/jpeg");

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenStreamIsEmpty_ReturnsFalse()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var allowed = await _sut.IsAllowedAsync(stream, "image/png");

        allowed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("IMAGE/PNG")]
    [InlineData("Image/Png")]
    [InlineData("image/PNG")]
    public async Task IsAllowedAsync_WhenDeclaredContentTypeCasingVaries_MatchesCaseInsensitively(string declared)
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var allowed = await _sut.IsAllowedAsync(stream, declared);

        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ResetsStreamPositionToZeroAfterInspection()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x11, 0x22 };
        using var stream = new MemoryStream(bytes);
        stream.Position = 4;

        _ = await _sut.IsAllowedAsync(stream, "image/png");

        stream.Position.ShouldBe(0);
    }

    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableStream(byte[] data) => _inner = new MemoryStream(data);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
