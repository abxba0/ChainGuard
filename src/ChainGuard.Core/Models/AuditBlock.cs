using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChainGuard.Core.Models;

/// <summary>
/// Represents a single block in the audit chain with tamper-evident properties.
/// </summary>
public class AuditBlock
{
    /// <summary>
    /// Unique identifier for this block.
    /// </summary>
    public Guid BlockId { get; set; }

    /// <summary>
    /// The height/index of this block in the chain (0 for genesis block).
    /// </summary>
    public int BlockHeight { get; set; }

    /// <summary>
    /// UTC timestamp when this block was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Hash of the previous block in the chain (null for genesis block).
    /// </summary>
    public string? PreviousHash { get; set; }

    /// <summary>
    /// Hash of this block's content.
    /// </summary>
    public string CurrentHash { get; private set; }

    /// <summary>
    /// Digital signature of this block.
    /// </summary>
    public string Signature { get; private set; }

    /// <summary>
    /// Nonce for replay protection.
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// Hash of the payload data (allows verification without exposing sensitive data).
    /// </summary>
    public string PayloadHash { get; set; }

    /// <summary>
    /// Optional payload data (serialized as JSON).
    /// </summary>
    public string? PayloadData { get; set; }

    /// <summary>
    /// Metadata about the payload (non-sensitive, searchable fields).
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Creates a new audit block.
    /// </summary>
    public AuditBlock()
    {
        BlockId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        Nonce = Guid.NewGuid().ToString();
        Metadata = new Dictionary<string, string>();
        CurrentHash = string.Empty;
        Signature = string.Empty;
        PayloadHash = string.Empty;
    }

    /// <summary>
    /// Calculates the hash of this block based on its contents.
    /// </summary>
    /// <returns>SHA-256 hash as hexadecimal string.</returns>
    public string CalculateHash()
    {
        var blockData = $"{BlockId}{BlockHeight}{Timestamp:O}{PreviousHash}{Nonce}{PayloadHash}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(blockData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Calculates the hash of the payload data.
    /// </summary>
    /// <param name="payload">The payload object to hash.</param>
    /// <returns>SHA-256 hash as hexadecimal string.</returns>
    public static string CalculatePayloadHash(object? payload)
    {
        if (payload == null)
            return string.Empty;

        var json = JsonSerializer.Serialize(payload);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Finalizes the block by calculating its hash.
    /// This should be called after all block properties are set.
    /// </summary>
    public void FinalizeBlock()
    {
        CurrentHash = CalculateHash();
    }

    /// <summary>
    /// Signs the block using RSA private key.
    /// </summary>
    /// <param name="rsa">RSA instance with private key loaded.</param>
    public void SignBlock(RSA rsa)
    {
        var dataToSign = Encoding.UTF8.GetBytes(CurrentHash);
        var signatureBytes = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Signature = Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Verifies the block's signature using RSA public key.
    /// </summary>
    /// <param name="rsa">RSA instance with public key loaded.</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    public bool VerifySignature(RSA rsa)
    {
        if (string.IsNullOrEmpty(Signature))
            return false;

        try
        {
            var dataToVerify = Encoding.UTF8.GetBytes(CurrentHash);
            var signatureBytes = Convert.FromBase64String(Signature);
            return rsa.VerifyData(dataToVerify, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies that the block's current hash is valid.
    /// </summary>
    /// <returns>True if the hash is valid, false otherwise.</returns>
    public bool VerifyHash()
    {
        return CurrentHash == CalculateHash();
    }

    /// <summary>
    /// Verifies that the payload hash matches the stored payload data.
    /// </summary>
    /// <returns>True if payload hash is valid, false otherwise.</returns>
    public bool VerifyPayloadHash()
    {
        if (string.IsNullOrEmpty(PayloadData))
            return string.IsNullOrEmpty(PayloadHash);

        var calculatedHash = CalculatePayloadHash(PayloadData);
        return PayloadHash == calculatedHash;
    }
}
