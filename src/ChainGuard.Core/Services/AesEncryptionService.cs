using System.Security.Cryptography;
using System.Text;

namespace ChainGuard.Core.Services;

/// <summary>
/// Implementation of AES-256-GCM encryption service for protecting sensitive off-chain data.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    /// <summary>
    /// Creates a new AES encryption service with the specified key.
    /// </summary>
    /// <param name="key">Base64-encoded 256-bit encryption key.</param>
    public AesEncryptionService(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        _key = Convert.FromBase64String(key);

        if (_key.Length != 32) // 256 bits
            throw new ArgumentException("Key must be 256 bits (32 bytes).", nameof(key));
    }

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <returns>Base64-encoded encrypted data (format: nonce|tag|ciphertext).</returns>
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentNullException(nameof(plaintext));

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

        // Generate a random nonce (12 bytes is standard for GCM)
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        // Allocate space for ciphertext and authentication tag
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        // Encrypt
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine nonce + tag + ciphertext
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts ciphertext that was encrypted using AES-256-GCM.
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted data (format: nonce|tag|ciphertext).</param>
    /// <returns>The decrypted plaintext.</returns>
    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            throw new ArgumentNullException(nameof(ciphertext));

        var data = Convert.FromBase64String(ciphertext);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

        // Extract nonce, tag, and ciphertext
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = new byte[nonceSize];
        var tag = new byte[tagSize];
        var encryptedBytes = new byte[data.Length - nonceSize - tagSize];

        Buffer.BlockCopy(data, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(data, nonceSize, tag, 0, tagSize);
        Buffer.BlockCopy(data, nonceSize + tagSize, encryptedBytes, 0, encryptedBytes.Length);

        // Decrypt
        var plaintextBytes = new byte[encryptedBytes.Length];
        aes.Decrypt(nonce, encryptedBytes, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    /// <summary>
    /// Generates a new 256-bit encryption key.
    /// </summary>
    /// <returns>Base64-encoded encryption key.</returns>
    public string GenerateKey()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
