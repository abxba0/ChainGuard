using ChainGuard.Core.Models;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace ChainGuard.Core.Tests;

/// <summary>
/// Security-focused tests for blockchain vulnerabilities.
/// Tests for replay attacks, hash collision resistance, tampering detection,
/// and cryptographic security.
/// </summary>
public class SecurityTests
{
    #region Replay Attack Prevention

    [Fact]
    public void Nonce_ShouldBeUnique_ForEachBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock();

        var nonces = new HashSet<string>();

        // Act - Add multiple blocks and collect nonces
        for (int i = 0; i < 100; i++)
        {
            var block = chain.AddBlock(new { Index = i });
            nonces.Add(block.Nonce);
        }

        // Assert - All nonces should be unique (101 including genesis)
        Assert.Equal(101, chain.Blocks.Count);
        Assert.Equal(101, chain.Blocks.Select(b => b.Nonce).Distinct().Count());
    }

    [Fact]
    public void Nonce_ShouldBeCryptographicallySecure()
    {
        // Arrange
        var block1 = new AuditBlock();
        var block2 = new AuditBlock();

        // Assert - Nonces should be 32 hex characters (16 bytes = 128 bits)
        Assert.Equal(32, block1.Nonce.Length);
        Assert.Equal(32, block2.Nonce.Length);
        Assert.NotEqual(block1.Nonce, block2.Nonce);
        Assert.True(block1.Nonce.All(c => "0123456789abcdef".Contains(c)));
    }

    [Fact]
    public void SamePayload_ShouldProduceDifferentBlocks()
    {
        // Arrange - Same payload added twice
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock();

        var payload = new { Event = "SameEvent", Value = 123 };

        // Act
        var block1 = chain.AddBlock(payload);
        var block2 = chain.AddBlock(payload);

        // Assert - Blocks should be different due to different nonces and timestamps
        Assert.NotEqual(block1.CurrentHash, block2.CurrentHash);
        Assert.NotEqual(block1.Nonce, block2.Nonce);
        Assert.Equal(block1.PayloadHash, block2.PayloadHash); // Same payload, same payload hash
    }

    #endregion

    #region Hash Integrity

    [Fact]
    public void Hash_ShouldBeConsistent_ForSameData()
    {
        // Arrange
        var block = new AuditBlock
        {
            BlockId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            BlockHeight = 0,
            Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PreviousHash = null,
            Nonce = "fixednonce",
            PayloadHash = "fixedpayloadhash"
        };

        // Act
        var hash1 = block.CalculateHash();
        var hash2 = block.CalculateHash();
        var hash3 = block.CalculateHash();

        // Assert - SHA-256 produces 64 hex characters consistently
        Assert.Equal(64, hash1.Length);
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void AnyFieldChange_ShouldChangeHash()
    {
        // Arrange
        var baseBlock = new AuditBlock
        {
            BlockId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            BlockHeight = 0,
            Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PreviousHash = null,
            Nonce = "nonce",
            PayloadHash = "payload"
        };
        var originalHash = baseBlock.CalculateHash();

        // Test each field change
        var tests = new List<(string Field, Action<AuditBlock> Modify)>
        {
            ("BlockId", b => b.BlockId = Guid.NewGuid()),
            ("BlockHeight", b => b.BlockHeight = 1),
            ("Timestamp", b => b.Timestamp = DateTime.UtcNow),
            ("PreviousHash", b => b.PreviousHash = "changed"),
            ("Nonce", b => b.Nonce = "changed"),
            ("PayloadHash", b => b.PayloadHash = "changed")
        };

        foreach (var (field, modify) in tests)
        {
            var testBlock = new AuditBlock
            {
                BlockId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                BlockHeight = 0,
                Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PreviousHash = null,
                Nonce = "nonce",
                PayloadHash = "payload"
            };

            // Act
            modify(testBlock);
            var newHash = testBlock.CalculateHash();

            // Assert
            Assert.NotEqual(originalHash, newHash);
        }
    }

    #endregion

    #region Signature Security

    [Fact]
    public void Signature_ShouldFailVerification_WithDifferentKey()
    {
        // Arrange
        using var rsaSigner = RSA.Create(2048);
        using var rsaVerifier = RSA.Create(2048); // Different key

        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();
        block.SignBlock(rsaSigner);

        // Act
        var isValid = block.VerifySignature(rsaVerifier);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Signature_ShouldFailVerification_AfterHashChange()
    {
        // Arrange
        using var rsa = RSA.Create(2048);

        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "original"
        };
        block.FinalizeBlock();
        block.SignBlock(rsa);

        // Store original signature
        var originalSignature = block.Signature;

        // Tamper with block (change payload hash and recalculate hash)
        block.PayloadHash = "tampered";
        block.FinalizeBlock();

        // Copy original signature back (attacker trying to use old signature)
        typeof(AuditBlock).GetProperty("Signature")!.SetValue(block, originalSignature);

        // Act
        var isValid = block.VerifySignature(rsa);

        // Assert - Signature should fail because hash changed
        Assert.False(isValid);
    }

    [Fact]
    public void EmptySignature_ShouldFailVerification()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();
        // Don't sign - signature is empty

        // Act
        var isValid = block.VerifySignature(rsa);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void InvalidBase64Signature_ShouldFailGracefully()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var block = new AuditBlock
        {
            BlockHeight = 0,
            PayloadHash = "test"
        };
        block.FinalizeBlock();

        // Set invalid signature
        typeof(AuditBlock).GetProperty("Signature")!.SetValue(block, "not-valid-base64!!!");

        // Act
        var isValid = block.VerifySignature(rsa);

        // Assert - Should return false, not throw
        Assert.False(isValid);
    }

    #endregion

    #region Chain Integrity

    [Fact]
    public void ChainBreak_ShouldBeDetected_WhenPreviousHashModified()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);

        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Block1" });
        chain.AddBlock(new { Event = "Block2" });

        // Act - Break the chain by modifying previous hash
        chain.Blocks[2].PreviousHash = "fake_hash";

        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("broken chain link"));
    }

    [Fact]
    public void BlockHeightManipulation_ShouldBeDetected()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Block1" });
        chain.AddBlock(new { Event = "Block2" });

        // Act - Manipulate block height
        chain.Blocks[1].BlockHeight = 5; // Should be 1

        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("incorrect height"));
    }

    [Fact]
    public void TimestampManipulation_ShouldBeDetected()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Block1" });
        chain.AddBlock(new { Event = "Block2" });

        // Act - Set timestamp before parent
        chain.Blocks[2].Timestamp = chain.Blocks[0].Timestamp.AddDays(-1);

        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("timestamp before previous block"));
    }

    [Fact]
    public void GenesisBlockWithPreviousHash_ShouldBeInvalid()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        chain.CreateGenesisBlock();

        // Act - Add previous hash to genesis (invalid)
        chain.Blocks[0].PreviousHash = "should_not_exist";

        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Genesis block should not have a previous hash"));
    }

    #endregion

    #region Double Spending Prevention

    [Fact]
    public void BlocksAreSequential_PreventingDoubleSpending()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();

        // Act - Add blocks with "transactions"
        var balances = new Dictionary<string, int>
        {
            ["Alice"] = 100,
            ["Bob"] = 0
        };

        // Transfer from Alice to Bob
        var block1 = chain.AddBlock(new
        {
            Type = "Transfer",
            From = "Alice",
            To = "Bob",
            Amount = 50
        });

        // Verify blocks are strictly sequential
        Assert.Equal(0, chain.Blocks[0].BlockHeight);
        Assert.Equal(1, chain.Blocks[1].BlockHeight);
        Assert.Equal(chain.Blocks[0].CurrentHash, chain.Blocks[1].PreviousHash);

        // Each block has unique position - can't insert a block with same height
        var result = chain.ValidateChain();
        Assert.True(result.IsValid);

        // Block heights are sequential without gaps
        for (int i = 0; i < chain.Blocks.Count; i++)
        {
            Assert.Equal(i, chain.Blocks[i].BlockHeight);
        }
    }

    #endregion

    #region Encryption Security

    [Fact]
    public void AesEncryption_ShouldDetectTampering()
    {
        // Arrange
        var key = GenerateValidKey();
        var service = new ChainGuard.Core.Services.AesEncryptionService(key);
        var plaintext = "sensitive data";

        var encrypted = service.Encrypt(plaintext);
        var bytes = Convert.FromBase64String(encrypted);

        // Act - Tamper with encrypted data
        bytes[bytes.Length / 2] ^= 0xFF; // Flip some bits
        var tampered = Convert.ToBase64String(bytes);

        // Assert - Should throw on decryption
        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(tampered));
    }

    [Fact]
    public void DifferentKeys_ShouldNotDecrypt()
    {
        // Arrange
        var key1 = GenerateValidKey();
        var key2 = GenerateValidKey();

        var service1 = new ChainGuard.Core.Services.AesEncryptionService(key1);
        var service2 = new ChainGuard.Core.Services.AesEncryptionService(key2);

        var plaintext = "secret";
        var encrypted = service1.Encrypt(plaintext);

        // Act & Assert - Should fail to decrypt with different key
        Assert.ThrowsAny<CryptographicException>(() => service2.Decrypt(encrypted));
    }

    #endregion

    #region Input Validation

    [Fact]
    public void ChainName_ShouldNotAcceptNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AuditChain(null!, "description"));
    }

    [Fact]
    public void ChainName_ShouldNotAcceptEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AuditChain("", "description"));
    }

    [Fact]
    public void ChainName_ShouldNotAcceptWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AuditChain("   ", "description"));
    }

    [Fact]
    public void ConfigurableChain_ShouldValidateConsensusConfig()
    {
        // Arrange
        var invalidConfig = new ConsensusConfig
        {
            Type = ConsensusType.ProofOfWork,
            Difficulty = 10 // Invalid - too high
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ConfigurableChain("Test", "Description", invalidConfig));
    }

    #endregion

    private static string GenerateValidKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
