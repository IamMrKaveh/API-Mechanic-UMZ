using Application.Cache.Contracts;
using Infrastructure.Cache.Options;
using Infrastructure.Cache.Redis.Services;
using Microsoft.Extensions.Options;
using SharedKernel.Attributes;

namespace Tests.Infrastructure.Cache.Redis.Services;

public class EncryptedRedisCacheServiceTests
{
    private readonly ICacheService _inner = Substitute.For<ICacheService>(); private readonly ILogger<EncryptedRedisCacheService> _logger = Substitute.For<ILogger<EncryptedRedisCacheService>>();

    private static string ValidKeyBase64() => Convert.ToBase64String(new byte[32]);

    private static IOptions<CacheEncryptionOptions> Options(CacheEncryptionOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    private EncryptedRedisCacheService CreateEnabledSut() =>
        new(_inner, Options(new CacheEncryptionOptions
        {
            IsEnabled = true,
            KeyBase64 = ValidKeyBase64(),
            KeyId = "v1"
        }), _logger);

    private EncryptedRedisCacheService CreateDisabledSut() =>
        new(_inner, Options(new CacheEncryptionOptions { IsEnabled = false }), _logger);

    [Fact]
    public void Constructor_WhenEncryptionEnabledButKeyBase64IsEmpty_ThrowsInvalidOperationException()
    {
        var options = Options(new CacheEncryptionOptions { IsEnabled = true, KeyBase64 = string.Empty });

        var act = () => new EncryptedRedisCacheService(_inner, options, _logger);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WhenEncryptionEnabledButKeyIsWrongLength_ThrowsInvalidOperationException()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);
        var options = Options(new CacheEncryptionOptions { IsEnabled = true, KeyBase64 = shortKey });

        var act = () => new EncryptedRedisCacheService(_inner, options, _logger);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WhenEncryptionDisabled_DoesNotRequireKey()
    {
        var act = () => new EncryptedRedisCacheService(
            _inner,
            Options(new CacheEncryptionOptions { IsEnabled = false, KeyBase64 = string.Empty }),
            _logger);

        act.ShouldNotThrow();
    }

    [Fact]
    public async Task GetAsync_ForNonSensitiveType_DelegatesToInnerCache()
    {
        var sut = CreateEnabledSut();
        _inner.GetAsync<string>("k", Arg.Any<CancellationToken>()).Returns("plain");

        var result = await sut.GetAsync<string>("k");

        result.ShouldBe("plain");
        await _inner.DidNotReceiveWithAnyArgs().GetAsync<EncryptedRedisCacheService.EncryptedPayload>(default!, default);
    }

    [Fact]
    public async Task SetAsync_ForNonSensitiveType_DelegatesToInnerCacheWithSameValueAndExpiry()
    {
        var sut = CreateEnabledSut();
        var expiry = TimeSpan.FromMinutes(3);

        await sut.SetAsync("k", "plain", expiry);

        await _inner.Received(1).SetAsync("k", "plain", expiry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_ForSensitiveType_WhenEncryptionDisabled_PassesThroughWithoutEncryption()
    {
        var sut = CreateDisabledSut();

        SensitiveDto? capturedRaw = null;
        await _inner.SetAsync(
            Arg.Any<string>(),
            Arg.Do<SensitiveDto>(v => capturedRaw = v),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());

        var payload = new SensitiveDto { Secret = "hunter2" };

        await sut.SetAsync("secret-k", payload);

        capturedRaw.ShouldNotBeNull();
        capturedRaw!.Secret.ShouldBe("hunter2");
        await _inner.DidNotReceiveWithAnyArgs()
            .SetAsync(default!, default(EncryptedRedisCacheService.EncryptedPayload)!, default, default);
    }

    [Fact]
    public async Task SetAsync_ForSensitiveType_WhenEncryptionEnabled_WritesEncryptedPayloadWithNonceCiphertextAndTag()
    {
        var sut = CreateEnabledSut();

        EncryptedRedisCacheService.EncryptedPayload? captured = null;
        await _inner.SetAsync(
            Arg.Any<string>(),
            Arg.Do<EncryptedRedisCacheService.EncryptedPayload>(p => captured = p),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());

        await sut.SetAsync("secret-k", new SensitiveDto { Secret = "hunter2" });

        captured.ShouldNotBeNull();
        captured!.Version.ShouldBe((byte)1);
        captured.KeyId.ShouldBe("v1");
        captured.NonceBase64.ShouldNotBeNullOrEmpty();
        captured.CiphertextBase64.ShouldNotBeNullOrEmpty();
        captured.TagBase64.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAsync_ForSensitiveType_RoundTripsThroughEncryptedPayload()
    {
        var sut = CreateEnabledSut();

        EncryptedRedisCacheService.EncryptedPayload? captured = null;
        await _inner.SetAsync(
            Arg.Any<string>(),
            Arg.Do<EncryptedRedisCacheService.EncryptedPayload>(p => captured = p),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());

        await sut.SetAsync("secret-k", new SensitiveDto { Secret = "hunter2" });

        captured.ShouldNotBeNull();
        _inner.GetAsync<EncryptedRedisCacheService.EncryptedPayload>("secret-k", Arg.Any<CancellationToken>())
            .Returns(captured);

        var decrypted = await sut.GetAsync<SensitiveDto>("secret-k");

        decrypted.ShouldNotBeNull();
        decrypted!.Secret.ShouldBe("hunter2");
    }

    [Fact]
    public async Task GetAsync_ForSensitiveType_WhenInnerReturnsNull_ReturnsDefault()
    {
        var sut = CreateEnabledSut();

        _inner.GetAsync<EncryptedRedisCacheService.EncryptedPayload>("secret-k", Arg.Any<CancellationToken>())
            .Returns((EncryptedRedisCacheService.EncryptedPayload?)null);

        var result = await sut.GetAsync<SensitiveDto>("secret-k");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_ForSensitiveType_WhenPayloadVersionIsUnsupported_ReturnsDefaultAndDoesNotThrow()
    {
        var sut = CreateEnabledSut();
        var badPayload = new EncryptedRedisCacheService.EncryptedPayload
        {
            Version = 99,
            KeyId = "v1",
            NonceBase64 = Convert.ToBase64String(new byte[12]),
            CiphertextBase64 = Convert.ToBase64String(new byte[8]),
            TagBase64 = Convert.ToBase64String(new byte[16])
        };

        _inner.GetAsync<EncryptedRedisCacheService.EncryptedPayload>("secret-k", Arg.Any<CancellationToken>())
            .Returns(badPayload);

        var result = await sut.GetAsync<SensitiveDto>("secret-k");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToInnerCache()
    {
        var sut = CreateEnabledSut();

        await sut.RemoveAsync("k");

        await _inner.Received(1).RemoveAsync("k", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveByPrefixAsync_DelegatesToInnerCache()
    {
        var sut = CreateEnabledSut();

        await sut.RemoveByPrefixAsync("prefix:");

        await _inner.Received(1).RemoveByPrefixAsync("prefix:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_DelegatesToInnerCacheResult()
    {
        var sut = CreateEnabledSut();
        _inner.ExistsAsync("k", Arg.Any<CancellationToken>()).Returns(true);

        var result = await sut.ExistsAsync("k");

        result.ShouldBeTrue();
        await _inner.Received(1).ExistsAsync("k", Arg.Any<CancellationToken>());
    }

    [Sensitive]
    public sealed class SensitiveDto
    {
        public string Secret { get; set; } = string.Empty;
    }
}
