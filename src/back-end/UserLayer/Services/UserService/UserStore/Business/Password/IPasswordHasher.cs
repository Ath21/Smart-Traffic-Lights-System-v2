namespace UserStore.Business.Password;

public interface IPasswordHasher
{
    bool VerifyPassword(string hash, string password);
    string HashPassword(string password);
}
