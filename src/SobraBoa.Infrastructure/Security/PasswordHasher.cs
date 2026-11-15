using System.Security.Cryptography;
using SobraBoa.Application.Interfaces;

namespace SobraBoa.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int Salt = 16, Key = 32, Iter = 100_000;

    public string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(Salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iter, HashAlgorithmName.SHA256, Key);
        return $"{Iter}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string senha, string armazenado)
    {
        var p = armazenado.Split('.');
        if (p.Length != 3) return false;
        var salt = Convert.FromBase64String(p[1]);
        var esperado = Convert.FromBase64String(p[2]);
        var calculado =
            Rfc2898DeriveBytes.Pbkdf2(senha, salt, int.Parse(p[0]), HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(esperado, calculado); // evita timing attack
    }
}