using Application.Payment.Contracts;

namespace Infrastructure.Payment.Services;

public sealed class PaymentCallbackNonceService(
    IServiceProvider serviceProvider,
    ICacheService cacheService,
    IAuditService auditService) : IPaymentCallbackNonceService
{
    private const string KeyPrefix = "payment:callback:nonce";

    private IConnectionMultiplexer? Redis => serviceProvider.GetService<IConnectionMultiplexer>();

    public async Task<string> IssueAsync(Guid paymentTransactionId, TimeSpan ttl, CancellationToken ct = default)
    {
        var effectiveTtl = ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : ttl;
        var nonce = GenerateNonce();
        var key = BuildKey(paymentTransactionId);

        var redis = Redis;
        if (redis is not null)
        {
            var db = redis.GetDatabase();
            var stored = await db.StringSetAsync(key, nonce, effectiveTtl, When.NotExists);
            if (!stored)
            {
                var existing = await db.StringGetAsync(key);
                if (existing.HasValue) return existing!;
                await db.StringSetAsync(key, nonce, effectiveTtl);
            }
        }
        else
        {
            var existing = await cacheService.GetAsync<string>(key, ct);
            if (!string.IsNullOrWhiteSpace(existing)) return existing;
            await cacheService.SetAsync(key, nonce, effectiveTtl, ct);
        }

        return nonce;
    }

    public async Task<bool> ValidateAndConsumeAsync(Guid paymentTransactionId, string nonce, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nonce)) return false;
        var key = BuildKey(paymentTransactionId);

        var redis = Redis;
        if (redis is not null)
        {
            var db = redis.GetDatabase();
            var stored = await db.StringGetDeleteAsync(key);
            if (!stored.HasValue)
            {
                await auditService.LogWarningAsync(
                    $"[PaymentCallbackNonce] Missing or already-consumed nonce for transaction {paymentTransactionId}.",
                    ct);
                return false;
            }

            var storedValue = stored.ToString();
            var isMatch = FixedTimeEquals(storedValue, nonce);
            if (!isMatch)
            {
                await auditService.LogSecurityEventAsync(
                    "PaymentCallbackNonceMismatch",
                    $"Nonce mismatch detected for transaction {paymentTransactionId}.",
                    IpAddress.Unknown,
                    null,
                    ct);
            }
            return isMatch;
        }

        var cached = await cacheService.GetAsync<string>(key, ct);
        if (string.IsNullOrWhiteSpace(cached))
        {
            await auditService.LogWarningAsync(
                $"[PaymentCallbackNonce] Missing or already-consumed nonce for transaction {paymentTransactionId}.",
                ct);
            return false;
        }
        await cacheService.RemoveAsync(key, ct);
        var matches = FixedTimeEquals(cached, nonce);
        if (!matches)
        {
            await auditService.LogSecurityEventAsync(
                "PaymentCallbackNonceMismatch",
                $"Nonce mismatch detected for transaction {paymentTransactionId}.",
                IpAddress.Unknown,
                null,
                ct);
        }
        return matches;
    }

    private static string BuildKey(Guid paymentTransactionId)
        => $"{KeyPrefix}:{paymentTransactionId:N}";

    private static string GenerateNonce()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left is null || right is null) return false;
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        if (leftBytes.Length != rightBytes.Length) return false;
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
