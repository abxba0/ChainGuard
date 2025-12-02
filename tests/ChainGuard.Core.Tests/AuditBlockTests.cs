using ChainGuard.Core.Models;
using System.Security.Cryptography;
using Xunit;

namespace ChainGuard.Core.Tests;

public class AuditBlockTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Act
        var block = new AuditBlock();

        // Assert
        Assert.NotEqual(Guid.Empty, block.BlockId);
        Assert.NotEqual(default(DateTime), block.Timestamp);
        Assert.NotNull(block.Nonce);
        Assert.NotNull(block.Metadata);
        Assert.Empty(block.Metadata);
        Assert.Equal(string.Empty, block.CurrentHash);
        Assert.Equal(string.Empty, block.Signature);
    }

    [Fact]
    public void CalculateHash_ShouldProduceDeterministicHash()
    {
        // Arrange
        var block = new AuditBlock
        {
            BlockId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            BlockHeight = 1,
            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PreviousHash = "previoushash",
            Nonce = "nonce123",
            PayloadHash = "payloadhash"
        };

        // Act
        var hash1 = block.CalculateHash();
        var hash2 = block.CalculateHash();

        // Assert
        Assert.NotEmpty(hash1);
        Assert.Equal(64, hash1.Length); // SHA-256 produces 64 hex characters
        Assert.Equal(hash1, hash2); // Should be deterministic
    }

    [Fact]
    public void CalculatePayloadHash_ShouldHandleNullPayload()
    {
        // Act
        var hash = AuditBlock.CalculatePayloadHash(null);

        // Assert
        Assert.Equal(string.Empty, hash);
    }

    [Fact]
    public void CalculatePayloadHash_ShouldHashPayloadCorrectly()
    {
        // Arrange
        var payload = new { UserId = 123, Action = "Login" };

        // Act
        var hash = AuditBlock.CalculatePayloadHash(payload);

        // Assert
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void FinalizeBlock_ShouldSetCurrentHash()
    {
        // Arrange
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PreviousHash = null,
            PayloadHash = "test"
        };

        // Act
        block.FinalizeBlock();

        // Assert
        Assert.NotEmpty(block.CurrentHash);
        Assert.Equal(64, block.CurrentHash.Length);
    }

    [Fact]
    public void SignBlock_ShouldCreateValidSignature()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();

        // Act
        block.SignBlock(rsa);

        // Assert
        Assert.NotEmpty(block.Signature);
    }

    [Fact]
    public void VerifySignature_ShouldReturnTrueForValidSignature()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();
        block.SignBlock(rsa);

        // Act
        var isValid = block.VerifySignature(rsa);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_ShouldReturnFalseForTamperedBlock()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();
        block.SignBlock(rsa);

        // Tamper with the block
        var tamperedBlock = new AuditBlock
        {
            BlockId = block.BlockId,
            BlockHeight = block.BlockHeight,
            Timestamp = block.Timestamp,
            PreviousHash = block.PreviousHash,
            Nonce = block.Nonce,
            PayloadHash = "tampered"
        };
        tamperedBlock.FinalizeBlock();

        // Copy signature from original block
        var signatureField = typeof(AuditBlock).GetProperty("Signature")!;
        signatureField.SetValue(tamperedBlock, block.Signature);

        // Act
        var isValid = tamperedBlock.VerifySignature(rsa);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyHash_ShouldReturnTrueForUntamperedBlock()
    {
        // Arrange
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();

        // Act
        var isValid = block.VerifyHash();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyHash_ShouldReturnFalseForTamperedBlock()
    {
        // Arrange
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();

        // Tamper with the block
        block.PayloadHash = "tampered";

        // Act
        var isValid = block.VerifyHash();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Metadata_ShouldAllowCustomFields()
    {
        // Arrange
        var block = new AuditBlock();

        // Act
        block.Metadata["EventType"] = "UserLogin";
        block.Metadata["UserId"] = "12345";

        // Assert
        Assert.Equal("UserLogin", block.Metadata["EventType"]);
        Assert.Equal("12345", block.Metadata["UserId"]);
        Assert.Equal(2, block.Metadata.Count);
    }
}
