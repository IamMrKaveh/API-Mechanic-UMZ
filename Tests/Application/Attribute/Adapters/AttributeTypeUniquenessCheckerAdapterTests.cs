using Application.Attribute.Adapters;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;

namespace Tests.Application.Attribute.Adapters;

public class AttributeTypeUniquenessCheckerAdapterTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>();
    private readonly AttributeTypeUniquenessCheckerAdapter _sut;

    public AttributeTypeUniquenessCheckerAdapterTests()
    {
        _sut = new AttributeTypeUniquenessCheckerAdapter(_repository);
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNameDoesNotExist_ReturnsTrue()
    {
        _repository.AttributeTypeExistsAsync("Color", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.IsUniqueAsync("Color");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsUniqueAsync_WhenNameExists_ReturnsFalse()
    {
        _repository.AttributeTypeExistsAsync("Color", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.IsUniqueAsync("Color");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsUniqueAsync_WithExcludeId_ForwardsExcludeIdToRepository()
    {
        var excludeId = AttributeTypeId.NewId();
        AttributeTypeId? captured = null;
        _repository.AttributeTypeExistsAsync(
                Arg.Any<string>(),
                Arg.Do<AttributeTypeId?>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.IsUniqueAsync("Size", excludeId);

        captured.ShouldBe(excludeId);
    }

    [Fact]
    public async Task IsUniqueAsync_ForwardsNameAndCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        string? capturedName = null;
        CancellationToken capturedCt = default;
        _repository.AttributeTypeExistsAsync(
                Arg.Do<string>(x => capturedName = x),
                Arg.Any<AttributeTypeId?>(),
                Arg.Do<CancellationToken>(x => capturedCt = x))
            .Returns(false);

        await _sut.IsUniqueAsync("Material", null, cts.Token);

        capturedName.ShouldBe("Material");
        capturedCt.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task IsUniqueAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repository.AttributeTypeExistsAsync(Arg.Any<string>(), Arg.Any<AttributeTypeId?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.IsUniqueAsync("Color"));
    }

    [Fact]
    public async Task IsUniqueAsync_DefaultExcludeId_IsNull()
    {
        AttributeTypeId? captured = AttributeTypeId.NewId();
        _repository.AttributeTypeExistsAsync(
                Arg.Any<string>(),
                Arg.Do<AttributeTypeId?>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.IsUniqueAsync("Color");

        captured.ShouldBeNull();
    }
}
