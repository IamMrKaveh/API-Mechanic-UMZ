namespace Presentation.Common.Cookies;

public interface IAuthCookieService
{
    string? ReadRefreshToken(HttpRequest request);

    void WriteRefreshToken(HttpResponse response, string refreshToken, DateTime expiresAtUtc);

    void WriteRefreshToken(HttpResponse response, string refreshToken);

    void ClearRefreshToken(HttpResponse response);
}
