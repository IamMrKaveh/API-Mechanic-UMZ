namespace Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(Domain.User.User user);
}