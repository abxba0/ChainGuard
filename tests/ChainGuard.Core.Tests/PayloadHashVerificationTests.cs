using ChainGuard.Core.Models;
using System.Text.Json;
using Xunit;

namespace ChainGuard.Core.Tests;

/// <summary>
/// Tests for the PayloadHash verification fix.
/// The bug was that VerifyPayloadHash() was double-serializing the payload,
/// causing verification to fail when PayloadData was already a JSON string.
/// </summary>
public class PayloadHashVerificationTests
{
    [Fact]
    public void VerifyPayloadHash_WithValidPayload_ShouldReturnTrue()
    {
        // Arrange
        var payload = new { UserId = 123, Action = "Login" };
        var block = new AuditBlock
        {
            BlockHeight = 1,
            PayloadHash = AuditBlock.CalculatePayloadHash(payload),
            PayloadData = JsonSerializer.Serialize(payload)
        };

        // Act
        var isValid = block.VerifyPayloadHash();

        // Assert
        Assert.True(isValid, "PayloadHash verification should succeed for correctly stored payload");
    }

    [Fact]
    public void VerifyPayloadHash_WithTamperedPayload_ShouldReturnFalse()
    {
        // Arrange
        var originalPayload = new { UserId = 123, Action = "Login" };
        var tamperedPayload = new { UserId = 456, Action = "Login" };
        var block = new AuditBlock
        {
            BlockHeight = 1,
            PayloadHash = AuditBlock.CalculatePayloadHash(originalPayload),
            PayloadData = JsonSerializer.Serialize(tamperedPayload) // Tampered!
        };

        // Act
        var isValid = block.VerifyPayloadHash();

        // Assert
        Assert.False(isValid, "PayloadHash verification should fail for tampered payload");
    }

    [Fact]
    public void VerifyPayloadHash_WithEmptyPayload_ShouldReturnTrue()
    {
        // Arrange
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = string.Empty,
            PayloadData = null
        };

        // Act
        var isValid = block.VerifyPayloadHash();

        // Assert
        Assert.True(isValid, "PayloadHash verification should succeed for empty payload");
    }

    [Fact]
    public void VerifyPayloadHash_WithMismatchedEmptyStates_ShouldReturnFalse()
    {
        // Arrange - PayloadHash is set but PayloadData is null
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "somehash",
            PayloadData = null
        };

        // Act
        var isValid = block.VerifyPayloadHash();

        // Assert
        Assert.False(isValid, "PayloadHash verification should fail when hash is set but data is null");
    }

    [Fact]
    public void VerifyPayloadHash_AfterChainAddBlock_ShouldReturnTrue()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock();
        var payload = new { Event = "UserRegistered", UserId = 42 };
        var block = chain.AddBlock(payload);

        // Act
        var isValid = block.VerifyPayloadHash();

        // Assert
        Assert.True(isValid, "PayloadHash should be verifiable after AddBlock");
    }

    [Fact]
    public void VerifyPayloadHash_WithComplexObject_ShouldReturnTrue()
    {
        // Arrange
        var payload = new
        {
            UserId = 123,
            Action = "Login",
            Details = new
            {
                IPAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                Timestamp = DateTime.UtcNow
            },
            Tags = new[] { "security", "audit" }
        };

        var block = new AuditBlock
        {
            BlockHeight = 1,
            PayloadHash = AuditBlock.CalculatePayloadHash(payload),
            PayloadData = JsonSerializer.Serialize(payload)
        };

        // Act
        var isValid = block.VerifyPayloadHash();

        // Assert
        Assert.True(isValid, "PayloadHash verification should work with complex nested objects");
    }

    [Fact]
    public void CalculatePayloadHash_ShouldBeConsistent()
    {
        // Arrange
        var payload = new { UserId = 123, Action = "Login" };

        // Act
        var hash1 = AuditBlock.CalculatePayloadHash(payload);
        var hash2 = AuditBlock.CalculatePayloadHash(payload);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 produces 64 hex characters
    }

    [Fact]
    public void CalculatePayloadHash_DifferentObjects_ShouldProduceDifferentHashes()
    {
        // Arrange
        var payload1 = new { UserId = 123, Action = "Login" };
        var payload2 = new { UserId = 456, Action = "Login" };

        // Act
        var hash1 = AuditBlock.CalculatePayloadHash(payload1);
        var hash2 = AuditBlock.CalculatePayloadHash(payload2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ChainValidation_WithValidPayloads_ShouldPass()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock(new { Event = "Genesis" });
        chain.AddBlock(new { Event = "Block1" });
        chain.AddBlock(new { Event = "Block2" });

        // Act - Verify all blocks have valid payload hashes
        foreach (var block in chain.Blocks)
        {
            var isValid = block.VerifyPayloadHash();

            // Assert
            Assert.True(isValid, $"Block at height {block.BlockHeight} should have valid payload hash");
        }
    }
}
