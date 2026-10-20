using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Presentation.Notification.Endpoints;

namespace Tests.Security;

/// <summary>
/// Phase 6 invariants (security audit) that are easiest to enforce by scanning the
/// production sources: no weak cryptography, no interpolated raw SQL, no CORS
/// wildcards, no trusting forwarded headers for rate limiting and no secrets in logs.
/// A violation fails the build with the offending file and line.
/// </summary>
public class SourceHygieneInvariantsTests
{
    private static IReadOnlyList<SecurityInvariantHelpers.SourceLine> Lines =>
        SecurityInvariantHelpers.ProductionSource;

    private static readonly string[] WeakCryptographyPatterns =
    [
        @"\bMD5(?:CryptoServiceProvider|Managed|Create)?\b",
        @"\bSHA1(?:CryptoServiceProvider|Managed|Create)?\b",
        @"\bTripleDES\b",
        @"\bDESCryptoServiceProvider\b",
        @"\bnew\s+Random\s*\(",
        @"\bRandom\.Shared\b",
        @"\bBinaryFormatter\b",
    ];

    private static readonly string[] RawSqlPatterns =
    [
        @"(?:ExecuteSqlRaw|ExecuteSqlRawAsync|FromSqlRaw|SqlQueryRaw)\s*\(\s*\$""",
        @"new\s+SqlCommand\s*\(\s*\$""",
        @"(?:\.Query|\.QueryAsync|\.Execute|\.ExecuteAsync)(?:<[^>]*>)?\s*\(\s*\$""",
    ];

    private static readonly string[] LoggingCallPatterns =
    [
        @"_logger\.",
        @"\bLog\.(?:Information|Warning|Error|Debug|Critical|Trace)\b",
        @"\blogger\.(?:LogInformation|LogWarning|LogError|LogDebug|LogCritical)\b",
    ];

    private static readonly string[] SensitiveWords =
    [
        "authorization",
        "bearer",
        "secretkey",
        "accesskey",
        "apikey",
        "otppepper",
        "password=",
    ];

    [Fact]
    public void ProductionCode_DoesNotUseWeakCryptographyOrBinaryFormatter()
    {
        var violations = SecurityInvariantHelpers.FindViolations(Lines, WeakCryptographyPatterns);

        violations.ShouldBeEmpty(
            "weak cryptography (MD5/SHA1/DES), System.Random and BinaryFormatter are forbidden " +
            "in production code (phase 6, item 9):" + Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ProductionCode_DoesNotInterpolateRawSql()
    {
        var violations = SecurityInvariantHelpers.FindViolations(Lines, RawSqlPatterns);

        violations.ShouldBeEmpty(
            "SQL must stay parameterized; interpolated raw SQL is forbidden (phase 6, item 5):" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ProductionCode_DoesNotUseCorsWildcards()
    {
        var violations = Lines
            .Where(line => Regex.IsMatch(
                SecurityInvariantHelpers.WithoutComment(line.Text),
                @"AllowAny(?:Origin|Header|Method)",
                RegexOptions.IgnoreCase))
            .Select(line => $"{line.File}:{line.Line}: {line.Text.Trim()}")
            .ToList();

        violations.ShouldBeEmpty(
            "CORS must list explicit origins/headers/methods with credentials (phase 6, item 3/SEC-12):" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void RateLimitMiddleware_BindsToConnectionIpAndIgnoresForwardedHeaders()
    {
        var rateLimitLines = Lines
            .Where(line => line.File.EndsWith("Presentation/Common/Middleware/RateLimitMiddleware.cs", StringComparison.Ordinal))
            .ToList();

        rateLimitLines.ShouldNotBeEmpty("RateLimitMiddleware.cs was not found in the scanned sources");

        rateLimitLines.Any(line => line.Text.Contains("Connection.RemoteIpAddress", StringComparison.Ordinal))
            .ShouldBeTrue("rate limiting must key on the connection IP (phase 6, item 10)");

        var forwardedHeaderUsage = rateLimitLines
            .Where(line => line.Text.Contains("X-Forwarded-For", StringComparison.OrdinalIgnoreCase))
            .Select(line => $"{line.File}:{line.Line}: {line.Text.Trim()}")
            .ToList();

        forwardedHeaderUsage.ShouldBeEmpty(
            "X-Forwarded-For is client-controlled and must not drive rate limiting until a trusted proxy is configured:" +
            Environment.NewLine + string.Join(Environment.NewLine, forwardedHeaderUsage));
    }

    [Fact]
    public void ProductionCode_DoesNotLogSecretsOrAuthorizationHeaders()
    {
        var violations = Lines
            .Where(line =>
                LoggingCallPatterns.Any(pattern => Regex.IsMatch(line.Text, pattern)) &&
                SensitiveWords.Any(word => line.Text.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(line => $"{line.File}:{line.Line}: {line.Text.Trim()}")
            .ToList();

        violations.ShouldBeEmpty(
            "log statements must never include credentials or authorization headers (phase 6, item 9):" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void PaymentCallbackNonce_IsNotBehindAFeatureFlag()
    {
        var paymentLines = Lines
            .Where(line => line.File.EndsWith("Infrastructure/Payment/Services/PaymentService.cs", StringComparison.Ordinal))
            .ToList();

        paymentLines.ShouldNotBeEmpty("PaymentService.cs was not found in the scanned sources");

        paymentLines.Any(line => line.Text.Contains("ValidateAndConsumeAsync", StringComparison.Ordinal))
            .ShouldBeTrue("the callback nonce must always be validated (SEC-06)");

        paymentLines.Any(line => line.Text.Contains("PaymentCallbackSignatureRequired", StringComparison.Ordinal))
            .ShouldBeFalse("the nonce check must not be gated behind a feature flag (SEC-06)");
    }

    [Fact]
    public void UnreadNotificationCount_IsNotAnonymous()
    {
        var method = typeof(NotificationsController).GetMethod("GetUnreadCount");

        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .ShouldBeEmpty("the unread-count endpoint requires authentication (SEC-16)");

        typeof(NotificationsController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .ShouldBeEmpty();
    }
}
