namespace ChainGuard.Core.Services;

/// <summary>
/// Service interface for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext data using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <returns>Base64-encoded encrypted data with nonce and tag.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts ciphertext that was encrypted using AES-256-GCM.
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted data with nonce and tag.</param>
    /// <returns>The decrypted plaintext.</returns>
    string Decrypt(string ciphertext);

    /// <summary>
    /// Generates a new encryption key.
    /// </summary>
    /// <returns>Base64-encoded encryption key.</returns>
    string GenerateKey();
}
