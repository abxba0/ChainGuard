using ChainGuard.Core.Models;
using System.Security.Cryptography;
using Xunit;

namespace ChainGuard.Core.Tests;

public class AuditChainTests
{
    [Fact]
    public void Constructor_ShouldInitializeChainCorrectly()
    {
        // Act
        var chain = new AuditChain("TestChain", "Test chain description");

        // Assert
        Assert.NotEqual(Guid.Empty, chain.ChainId);
        Assert.Equal("TestChain", chain.ChainName);
        Assert.Equal("Test chain description", chain.Description);
        Assert.Empty(chain.Blocks);
        Assert.True(chain.IsActive);
    }

    [Fact]
    public void CreateGenesisBlock_ShouldCreateFirstBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);

        // Act
        var genesisBlock = chain.CreateGenesisBlock(new { Event = "ChainCreated" });

        // Assert
        Assert.NotNull(genesisBlock);
        Assert.Equal(0, genesisBlock.BlockHeight);
        Assert.Null(genesisBlock.PreviousHash);
        Assert.NotEmpty(genesisBlock.CurrentHash);
        Assert.NotEmpty(genesisBlock.Signature);
        Assert.Equal("Genesis", genesisBlock.Metadata["Type"]);
        Assert.Single(chain.Blocks);
    }

    [Fact]
    public void CreateGenesisBlock_ShouldThrowIfCalledTwice()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => chain.CreateGenesisBlock());
    }

    [Fact]
    public void AddBlock_ShouldAddBlockToChain()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();

        // Act
        var newBlock = chain.AddBlock(new { UserId = 123, Action = "Login" });

        // Assert
        Assert.NotNull(newBlock);
        Assert.Equal(1, newBlock.BlockHeight);
        Assert.NotEmpty(newBlock.PreviousHash);
        Assert.NotEmpty(newBlock.CurrentHash);
        Assert.NotEmpty(newBlock.Signature);
        Assert.Equal(2, chain.Blocks.Count);
    }

    [Fact]
    public void AddBlock_ShouldThrowIfNoGenesisBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            chain.AddBlock(new { UserId = 123, Action = "Login" }));
    }

    [Fact]
    public void AddBlock_ShouldLinkToPreviousBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();

        // Act
        var block1 = chain.AddBlock(new { Event = "Event1" });
        var block2 = chain.AddBlock(new { Event = "Event2" });

        // Assert
        Assert.Equal(chain.Blocks[0].CurrentHash, block1.PreviousHash);
        Assert.Equal(block1.CurrentHash, block2.PreviousHash);
    }

    [Fact]
    public void AddBlock_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();
        var metadata = new Dictionary<string, string>
        {
            { "EventType", "UserLogin" },
            { "UserId", "12345" }
        };

        // Act
        var block = chain.AddBlock(new { Action = "Login" }, metadata);

        // Assert
        Assert.Equal("UserLogin", block.Metadata["EventType"]);
        Assert.Equal("12345", block.Metadata["UserId"]);
    }

    [Fact]
    public void ValidateChain_ShouldReturnValidForIntactChain()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Event1" });
        chain.AddBlock(new { Event = "Event2" });

        // Act
        var result = chain.ValidateChain();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TotalBlocks);
        Assert.Empty(result.Errors);
        Assert.Empty(result.InvalidBlocks);
    }

    [Fact]
    public void ValidateChain_ShouldDetectTamperedBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Event1" });

        // Tamper with a block
        chain.Blocks[1].PayloadHash = "tampered";

        // Act
        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.InvalidBlocks, id => id == chain.Blocks[1].BlockId);
    }

    [Fact]
    public void ValidateChain_ShouldDetectBrokenChainLink()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Event1" });

        // Break the chain link
        chain.Blocks[1].PreviousHash = "incorrecthash";

        // Act
        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("broken chain link"));
    }

    [Fact]
    public void ValidateChain_ShouldDetectInvalidGenesisBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();

        // Make genesis block invalid
        chain.Blocks[0].PreviousHash = "shouldbenull";

        // Act
        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Genesis block should not have a previous hash"));
    }

    [Fact]
    public void ValidateChain_ShouldDetectTimestampSequenceViolation()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Event1" });

        // Set invalid timestamp
        chain.Blocks[1].Timestamp = chain.Blocks[0].Timestamp.AddMinutes(-1);

        // Act
        var result = chain.ValidateChain();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("timestamp before previous block"));
    }

    [Fact]
    public void GetLatestBlock_ShouldReturnLastBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();
        var lastBlock = chain.AddBlock(new { Event = "LastEvent" });

        // Act
        var result = chain.GetLatestBlock();

        // Assert
        Assert.Equal(lastBlock.BlockId, result?.BlockId);
    }

    [Fact]
    public void GetLatestBlock_ShouldReturnNullForEmptyChain()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");

        // Act
        var result = chain.GetLatestBlock();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBlockByHeight_ShouldReturnCorrectBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();
        var block1 = chain.AddBlock(new { Event = "Event1" });
        chain.AddBlock(new { Event = "Event2" });

        // Act
        var result = chain.GetBlockByHeight(1);

        // Assert
        Assert.Equal(block1.BlockId, result?.BlockId);
    }

    [Fact]
    public void GetBlockById_ShouldReturnCorrectBlock()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();
        var block1 = chain.AddBlock(new { Event = "Event1" });
        chain.AddBlock(new { Event = "Event2" });

        // Act
        var result = chain.GetBlockById(block1.BlockId);

        // Assert
        Assert.Equal(block1.BlockHeight, result?.BlockHeight);
    }

    [Fact]
    public void MultipleBlocks_ShouldMaintainChainIntegrity()
    {
        // Arrange
        var chain = new AuditChain("TestChain", "Test description");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();

        // Act - Add 100 blocks
        for (int i = 0; i < 100; i++)
        {
            chain.AddBlock(new { Index = i, Event = $"Event{i}" });
        }

        // Assert
        Assert.Equal(101, chain.Blocks.Count); // 100 + genesis
        var validationResult = chain.ValidateChain();
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }
}
