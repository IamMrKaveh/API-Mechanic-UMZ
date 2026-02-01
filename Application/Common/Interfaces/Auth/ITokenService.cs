namespace Application.Common.Interfaces.Auth;

public interface ITokenService
{
    string GenerateJwtToken(Domain.User.User user);
}