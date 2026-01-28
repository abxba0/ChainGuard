using ChainGuard.Core.Models;
using System.Security.Cryptography;
using Xunit;

namespace ChainGuard.Core.Tests;

public class ConfigurableChainTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateChain()
    {
        // Arrange & Act
        var chain = new ConfigurableChain("TestChain", "Test description");

        // Assert
        Assert.NotEqual(Guid.Empty, chain.ChainId);
        Assert.Equal("TestChain", chain.ChainName);
        Assert.Equal("Test description", chain.Description);
        Assert.Empty(chain.Blocks);
        Assert.True(chain.IsActive);
        Assert.NotNull(chain.Consensus);
        Assert.Equal(ConsensusType.None, chain.Consensus.Type);
    }

    [Fact]
    public void Constructor_WithNullChainName_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConfigurableChain(null!, "description"));
    }

    [Fact]
    public void Constructor_WithEmptyChainName_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConfigurableChain("", "description"));
    }

    [Fact]
    public void Constructor_WithProofOfWorkConsensus_ShouldCreateChain()
    {
        // Arrange
        var consensus = ConsensusConfig.ProofOfWork(difficulty: 2);

        // Act
        var chain = new ConfigurableChain("PoWChain", "PoW test", consensus);

        // Assert
        Assert.Equal(ConsensusType.ProofOfWork, chain.Consensus.Type);
        Assert.Equal(2, chain.Consensus.Difficulty);
    }

    [Fact]
    public void Constructor_WithInvalidConsensus_ShouldThrow()
    {
        // Arrange
        var consensus = new ConsensusConfig
        {
            Type = ConsensusType.ProofOfWork,
            Difficulty = 10 // Invalid - too high
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConfigurableChain("TestChain", "description", consensus));
    }

    [Fact]
    public void CreateGenesisBlock_ShouldCreateFirstBlock()
    {
        // Arrange
        var chain = new ConfigurableChain("TestChain", "description");
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
    public void CreateGenesisBlock_WhenAlreadyExists_ShouldThrow()
    {
        // Arrange
        var chain = new ConfigurableChain("TestChain", "description");
        chain.CreateGenesisBlock();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => chain.CreateGenesisBlock());
    }

    [Fact]
    public void AddBlock_WithProofOfWork_ShouldMineBlock()
    {
        // Arrange
        var consensus = ConsensusConfig.ProofOfWork(difficulty: 1); // Low difficulty for test
        var chain = new ConfigurableChain("PoWChain", "description", consensus);
        chain.CreateGenesisBlock();

        // Act
        var block = chain.AddBlock(new { Event = "Test" });

        // Assert
        Assert.StartsWith("0", block.CurrentHash);
        Assert.Contains("MiningAttempts", block.Metadata.Keys);
        Assert.Contains("MiningTimeMs", block.Metadata.Keys);
    }

    [Fact]
    public void ValidateChain_WithProofOfWork_ShouldValidateDifficulty()
    {
        // Arrange
        var consensus = ConsensusConfig.ProofOfWork(difficulty: 1);
        var chain = new ConfigurableChain("PoWChain", "description", consensus);
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Test" });

        // Act
        var result = chain.ValidateChain();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.TotalBlocks);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AddBlock_WithMaxPayloadSize_ShouldEnforceLimit()
    {
        // Arrange
        var consensus = new ConsensusConfig
        {
            Type = ConsensusType.None,
            MaxPayloadSizeBytes = 100
        };
        var chain = new ConfigurableChain("TestChain", "description", consensus);
        chain.CreateGenesisBlock();

        // Create a large payload that exceeds the limit
        var largePayload = new { Data = new string('A', 200) };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => chain.AddBlock(largePayload));
    }

    [Fact]
    public void GetHashRate_WithPoWChain_ShouldCalculateRate()
    {
        // Arrange
        var consensus = ConsensusConfig.ProofOfWork(difficulty: 1);
        var chain = new ConfigurableChain("PoWChain", "description", consensus);
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Test1" });
        chain.AddBlock(new { Event = "Test2" });

        // Act
        var hashRate = chain.GetHashRate();

        // Assert
        Assert.True(hashRate > 0);
    }

    [Fact]
    public void GetHashRate_WithNonPoWChain_ShouldReturnZero()
    {
        // Arrange
        var chain = new ConfigurableChain("TestChain", "description");
        chain.CreateGenesisBlock();
        chain.AddBlock(new { Event = "Test" });

        // Act
        var hashRate = chain.GetHashRate();

        // Assert
        Assert.Equal(0, hashRate);
    }

    [Fact]
    public void Statistics_ShouldTrackBlockData()
    {
        // Arrange
        var chain = new ConfigurableChain("TestChain", "description");
        chain.CreateGenesisBlock();

        // Act
        chain.AddBlock(new { Event = "Test1" });
        chain.AddBlock(new { Event = "Test2" });

        // Assert
        Assert.Equal(3, chain.Statistics.TotalBlocks);
        Assert.Equal(2, chain.Statistics.LastBlockHeight);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentBlockAddition_ShouldMaintainIntegrity()
    {
        // Arrange
        var chain = new ConfigurableChain("TestChain", "description");
        using var rsa = RSA.Create(2048);
        chain.SetRSA(rsa);
        chain.CreateGenesisBlock();

        // Act - Add blocks concurrently
        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() => chain.AddBlock(new { Index = i }))).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Chain should be valid and have correct number of blocks
        var result = chain.ValidateChain();
        Assert.True(result.IsValid);
        Assert.Equal(11, chain.Blocks.Count); // Genesis + 10 blocks
    }
}

public class ConsensusConfigTests
{
    [Fact]
    public void Default_ShouldReturnNoConsensus()
    {
        // Act
        var config = ConsensusConfig.Default;

        // Assert
        Assert.Equal(ConsensusType.None, config.Type);
        Assert.True(config.RequireSignatures);
    }

    [Fact]
    public void ProofOfWork_ShouldSetCorrectDefaults()
    {
        // Act
        var config = ConsensusConfig.ProofOfWork(difficulty: 3, targetBlockTimeSeconds: 15);

        // Assert
        Assert.Equal(ConsensusType.ProofOfWork, config.Type);
        Assert.Equal(3, config.Difficulty);
        Assert.Equal(15, config.TargetBlockTimeSeconds);
        Assert.True(config.RequireSignatures);
    }

    [Fact]
    public void ProofOfWork_ShouldClampDifficulty()
    {
        // Act
        var tooHigh = ConsensusConfig.ProofOfWork(difficulty: 10);
        var tooLow = ConsensusConfig.ProofOfWork(difficulty: -1);

        // Assert
        Assert.Equal(6, tooHigh.Difficulty);
        Assert.Equal(1, tooLow.Difficulty);
    }

    [Fact]
    public void ProofOfAuthority_ShouldSetValidators()
    {
        // Arrange
        var validators = new[] { "validator1", "validator2" };

        // Act
        var config = ConsensusConfig.ProofOfAuthority(validators);

        // Assert
        Assert.Equal(ConsensusType.ProofOfAuthority, config.Type);
        Assert.Equal(2, config.AuthorizedValidators.Count);
    }

    [Fact]
    public void Validate_WithInvalidPoAConfig_ShouldReturnErrors()
    {
        // Arrange
        var config = new ConsensusConfig
        {
            Type = ConsensusType.ProofOfAuthority,
            AuthorizedValidators = [] // Empty
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Single(errors);
        Assert.Contains("at least one authorized validator", errors[0].ToLower());
    }

    [Fact]
    public void Validate_WithInvalidPoWConfig_ShouldReturnErrors()
    {
        // Arrange
        var config = new ConsensusConfig
        {
            Type = ConsensusType.ProofOfWork,
            Difficulty = 10, // Too high
            TargetBlockTimeSeconds = 0 // Invalid
        };

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnNoErrors()
    {
        // Arrange
        var config = ConsensusConfig.ProofOfWork(difficulty: 2);

        // Act
        var errors = config.Validate();

        // Assert
        Assert.Empty(errors);
    }
}
