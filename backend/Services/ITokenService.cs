using MentalHealth.API.Models;

namespace MentalHealth.API.Services;

public interface ITokenService
{
    string CreateToken(User user);
}