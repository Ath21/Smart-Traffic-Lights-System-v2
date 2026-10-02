using UserData.Entities;

namespace UserStore.Business.Token;

public interface ITokenService
{
    (string token, DateTime expiration) GenerateToken(UserEntity user);
}
