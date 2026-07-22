using SharedKernel.Attributes;

namespace Infrastructure.Cache.Redis.Services;

public sealed class EncryptedRedisCacheService : ICacheService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const byte FormatVersion = 1;

    private readonly ICacheService _inner;
    private readonly CacheEncryptionOptions _options;
    private readonly ILogger<EncryptedRedisCacheService> _logger;
    private readonly byte[]? _key;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EncryptedRedisCacheService(
        ICacheService inner,
        IOptions<CacheEncryptionOptions> options,
        ILogger<EncryptedRedisCacheService> logger)
    {
        _inner = inner;
        _options = options.Value;
        _logger = logger;

        if (_options.IsEnabled)
        {
            if (string.IsNullOrWhiteSpace(_options.KeyBase64))
                throw new InvalidOperationException("Cache:Encryption:KeyBase64 is required when encryption is enabled.");

            _key = Convert.FromBase64String(_options.KeyBase64);
            if (_key.Length != 32)
                throw new InvalidOperationException("Cache:Encryption:KeyBase64 must decode to a 256-bit (32-byte) key.");
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!ShouldEncrypt(typeof(T)))
            return await _inner.GetAsync<T>(key, ct);

        var payload = await _inner.GetAsync<EncryptedPayload>(key, ct);
        if (payload is null)
            return default;

        try
        {
            var plaintext = Decrypt(payload);
            return JsonSerializer.Deserialize<T>(plaintext, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt cached value for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (!ShouldEncrypt(typeof(T)))
        {
            await _inner.SetAsync(key, value, expiry, ct);
            return;
        }

        try
        {
            var serialized = JsonSerializer.Serialize(value, SerializerOptions);
            var encrypted = Encrypt(serialized);
            await _inner.SetAsync(key, encrypted, expiry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to encrypt cache value for key {Key}", key);
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _inner.RemoveAsync(key, ct);

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        => _inner.RemoveByPrefixAsync(prefix, ct);

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => _inner.ExistsAsync(key, ct);

    private bool ShouldEncrypt(Type type)
    {
        if (!_options.IsEnabled || _key is null)
            return false;

        if (type == typeof(EncryptedPayload))
            return false;

        return HasSensitiveMarker(type);
    }

    private static bool HasSensitiveMarker(Type type)
    {
        var current = type;
        while (current is not null && current != typeof(object))
        {
            if (current.GetCustomAttributes(typeof(SensitiveAttribute), inherit: false).Length > 0)
                return true;

            foreach (var prop in current.GetProperties())
            {
                if (prop.GetCustomAttributes(typeof(SensitiveAttribute), inherit: false).Length > 0)
                    return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private EncryptedPayload Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key!, TagSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag, associatedData: null);

        return new EncryptedPayload
        {
            Version = FormatVersion,
            KeyId = _options.KeyId,
            NonceBase64 = Convert.ToBase64String(nonce),
            CiphertextBase64 = Convert.ToBase64String(ciphertext),
            TagBase64 = Convert.ToBase64String(tag)
        };
    }

    private string Decrypt(EncryptedPayload payload)
    {
        if (payload.Version != FormatVersion)
            throw new InvalidOperationException($"Unsupported encrypted payload version: {payload.Version}.");

        var nonce = Convert.FromBase64String(payload.NonceBase64);
        var ciphertext = Convert.FromBase64String(payload.CiphertextBase64);
        var tag = Convert.FromBase64String(payload.TagBase64);
        var plainBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key!, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes, associatedData: null);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public sealed class EncryptedPayload
    {
        public byte Version { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public string NonceBase64 { get; set; } = string.Empty;
        public string CiphertextBase64 { get; set; } = string.Empty;
        public string TagBase64 { get; set; } = string.Empty;
    }
}
