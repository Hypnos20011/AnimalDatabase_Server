using System.Security.Cryptography;
using System.Text;

namespace AnimalDatabase.Api.Security;

public static class AdminKey
{
    public const string HeaderName = "X-Admin-Key";
    public const string ConfigurationKey = "Admin:ApiKey";

    public static bool Matches(string configured, string? provided)
    {
        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}