using ChainGuard.Core.Services;
using System.Security.Cryptography;
using Xunit;

namespace ChainGuard.Core.Tests;

public class AesEncryptionServiceTests
{
    [Fact]
    public void GenerateKey_ShouldReturnValidBase64Key()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());

        // Act
        var key = service.GenerateKey();

        // Assert
        Assert.NotNull(key);
        Assert.NotEmpty(key);
        
        // Should be valid base64
        var keyBytes = Convert.FromBase64String(key);
        Assert.Equal(32, keyBytes.Length); // 256 bits
    }

    [Fact]
    public void Constructor_WithValidKey_ShouldSucceed()
    {
        // Arrange
        var key = GenerateValidKey();

        // Act
        var service = new AesEncryptionService(key);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AesEncryptionService(null!));
    }

    [Fact]
    public void Constructor_WithInvalidKeyLength_ShouldThrow()
    {
        // Arrange
        var invalidKey = Convert.ToBase64String(new byte[16]); // 128 bits, not 256

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AesEncryptionService(invalidKey));
    }

    [Fact]
    public void Encrypt_ShouldReturnBase64String()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = "Hello, World!";

        // Act
        var encrypted = service.Encrypt(plaintext);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        
        // Should be valid base64
        var bytes = Convert.FromBase64String(encrypted);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void Encrypt_WithNullPlaintext_ShouldThrow()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Encrypt(null!));
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalPlaintext()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = "Hello, World!";
        var encrypted = service.Encrypt(plaintext);

        // Act
        var decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithNullCiphertext_ShouldThrow()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Decrypt(null!));
    }

    [Fact]
    public void Encrypt_SamePlaintextTwice_ShouldProduceDifferentCiphertext()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = "Hello, World!";

        // Act
        var encrypted1 = service.Encrypt(plaintext);
        var encrypted2 = service.Encrypt(plaintext);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2); // Different nonces should produce different ciphertext
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_ShouldWork()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = new string('A', 10000); // 10KB of text

        // Act
        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = "Hello, 世界! 🌍 Special: @#$%^&*()";

        // Act
        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithJson_ShouldWork()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = @"{""userId"": 12345, ""action"": ""Login"", ""timestamp"": ""2025-10-14T15:00:00Z""}";

        // Act
        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_ShouldThrow()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateValidKey());
        var plaintext = "Hello, World!";
        var encrypted = service.Encrypt(plaintext);
        
        // Tamper with the encrypted data
        var bytes = Convert.FromBase64String(encrypted);
        bytes[bytes.Length - 1] ^= 0xFF; // Flip bits in last byte
        var tamperedEncrypted = Convert.ToBase64String(bytes);

        // Act & Assert
        // AuthenticationTagMismatchException inherits from CryptographicException
        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(tamperedEncrypted));
    }

    private static string GenerateValidKey()
    {
        var key = new byte[32]; // 256 bits
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
