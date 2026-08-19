using LzDetailingStore.Domain.Entities.Identity;

namespace LzDetailingStore.Application.Interfaces;

public interface IJwtService
{
    (string AccessToken, int ExpiresIn) GenerateToken(User user, IList<string> roles);
}
