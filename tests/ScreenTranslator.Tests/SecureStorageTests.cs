using ScreenTranslator.Core.Services.Security;

namespace ScreenTranslator.Tests;

public class SecureStorageTests
{
    [Fact]
    public void Encrypt_RoundTrip_ReturnsOriginal()
    {
        var original = "sk-test-api-key-12345";
        var encrypted = SecureStorage.Encrypt(original);
        var decrypted = SecureStorage.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesEncPrefix()
    {
        var encrypted = SecureStorage.Encrypt("secret");
        Assert.StartsWith("enc:", encrypted);
    }

    [Fact]
    public void Decrypt_PlaintextInput_ReturnsSameString()
    {
        var plaintext = "my-plain-api-key";
        var result = SecureStorage.Decrypt(plaintext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", SecureStorage.Encrypt(""));
    }

    [Fact]
    public void Decrypt_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", SecureStorage.Decrypt(""));
    }

    [Fact]
    public void Encrypt_Null_ReturnsNull()
    {
        Assert.Null(SecureStorage.Encrypt(null!));
    }

    [Fact]
    public void IsEncrypted_EncryptedValue_ReturnsTrue()
    {
        var encrypted = SecureStorage.Encrypt("test");
        Assert.True(SecureStorage.IsEncrypted(encrypted));
    }

    [Fact]
    public void IsEncrypted_PlaintextValue_ReturnsFalse()
    {
        Assert.False(SecureStorage.IsEncrypted("plain-key"));
    }

    [Fact]
    public void IsEncrypted_Null_ReturnsFalse()
    {
        Assert.False(SecureStorage.IsEncrypted(null!));
    }

    [Fact]
    public void Encrypt_UnicodeText_RoundTrips()
    {
        var original = "ключ-апи-тест-123";
        var encrypted = SecureStorage.Encrypt(original);
        var decrypted = SecureStorage.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }
}
