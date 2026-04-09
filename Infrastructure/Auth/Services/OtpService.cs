using Application.Auth.Contracts;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Services;

public class OtpService : IOtpService
{
    /// <summary>
    /// تولید کد OTP امن با استفاده از الگوریتم امن رمزنگاری
    /// </summary>
    public string GenerateSecureOtp()
    {
        Span<char> buffer = stackalloc char[6];
        Span<char> digits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
        int available = 10;

        Span<byte> rnd = stackalloc byte[1];

        for (int i = 0; i < 6; i++)
        {
            RandomNumberGenerator.Fill(rnd);
            int index = rnd[0] % available;
            buffer[i] = digits[index];
            digits[index] = digits[available - 1];
            available--;
        }

        return new string(buffer);
    }

    /// <summary>
    /// هش کردن کد OTP با استفاده از SHA256
    /// </summary>
    public string HashOtp(OtpCode otp)
    {
        if (string.IsNullOrWhiteSpace(otp))
            throw new ArgumentNullException(nameof(otp), "کد OTP نمی‌تواند خالی باشد.");

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(otp);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    public Task<bool> SendOtpAsync(PhoneNumber phoneNumber, OtpCode code, OtpPurpose purpose, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateRateLimitAsync(UserId userId, OtpPurpose purpose, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// تأیید کد OTP
    /// </summary>
    public bool VerifyOtp(OtpCode otp, string hash)
    {
        if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(hash))
            return false;

        var computedHash = HashOtp(otp);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(hash));
    }
}