using System.Security.Cryptography;
using System.Text;

namespace ScreenTranslator.Core.Services.Security;

/// <summary>
/// Encrypts/decrypts strings using Windows DPAPI (current-user scope).
/// Encrypted values are prefixed with "enc:" followed by base64.
/// Plaintext values (without prefix) are returned as-is for backward compatibility.
/// </summary>
public static class SecureStorage
{
    private const string EncryptedPrefix = "enc:";

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return EncryptedPrefix + Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (!IsEncrypted(value))
            return value; // plaintext — backward compatibility

        var base64 = value[EncryptedPrefix.Length..];
        var encryptedBytes = Convert.FromBase64String(base64);
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public static bool IsEncrypted(string value)
        => value is not null && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
}
