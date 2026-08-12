using Application.Media.Features.Commands.UploadMedia;

namespace Tests.Application.Media.Features.Commands.UploadMedia;

public class UploadMediaValidatorTests
{
    private readonly UploadMediaValidator _sut = new();

    private static UploadMediaCommand BuildCommand(
        string fileName = "photo.jpg",
        string contentType = "image/jpeg",
        long fileSize = 1024,
        string entityType = "Product",
        Guid? entityId = null,
        bool isPrimary = false,
        string? altText = null)
    {
        return new UploadMediaCommand(
            Stream.Null,
            fileName,
            contentType,
            fileSize,
            entityType,
            entityId ?? Guid.NewGuid(),
            isPrimary,
            altText);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Validate_WhenContentTypeIsAllowedImage_IsValid(string contentType)
    {
        var command = BuildCommand(contentType: contentType);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/bmp")]
    [InlineData("image/svg+xml")]
    [InlineData("video/mp4")]
    [InlineData("text/plain")]
    [InlineData("")]
    public void Validate_WhenContentTypeIsNotAllowed_IsInvalidOnContentType(string contentType)
    {
        var command = BuildCommand(contentType: contentType);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadMediaCommand.ContentType));
    }

    [Fact]
    public void Validate_WhenFileSizeEqualsTenMegabytes_IsValid()
    {
        var command = BuildCommand(fileSize: 10L * 1024 * 1024);

        var result = _sut.Validate(command);

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(UploadMediaCommand.FileSize));
    }

    [Fact]
    public void Validate_WhenFileSizeExceedsTenMegabytes_IsInvalidOnFileSize()
    {
        var command = BuildCommand(fileSize: (10L * 1024 * 1024) + 1);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadMediaCommand.FileSize));
    }

    [Fact]
    public void Validate_WhenFileNameIsEmpty_IsInvalidOnFileName()
    {
        var command = BuildCommand(fileName: string.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadMediaCommand.FileName));
    }

    [Fact]
    public void Validate_WhenEntityTypeIsEmpty_IsInvalidOnEntityType()
    {
        var command = BuildCommand(entityType: string.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadMediaCommand.EntityType));
    }

    [Fact]
    public void Validate_WhenEntityIdIsEmpty_IsInvalidOnEntityId()
    {
        var command = BuildCommand(entityId: Guid.Empty);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadMediaCommand.EntityId));
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_IsValid()
    {
        var command = BuildCommand();

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
