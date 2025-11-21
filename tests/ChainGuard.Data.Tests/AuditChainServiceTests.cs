using ChainGuard.Data;
using ChainGuard.Data.Repositories;
using ChainGuard.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Xunit;

namespace ChainGuard.Data.Tests;

public class AuditChainServiceTests : IDisposable
{
    private readonly ChainGuardDbContext _context;
    private readonly AuditChainService _service;
    private readonly RSA _rsa;

    public AuditChainServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChainGuardDbContext>()
            .UseSqlite($"Data Source=test_{Guid.NewGuid()}.db")
            .Options;

        _context = new ChainGuardDbContext(options);
        _context.Database.Migrate(); // Apply migrations
        
        _rsa = RSA.Create(2048);
        var chainRepo = new ChainRepository(_context);
        var blockRepo = new BlockRepository(_context);
        var offChainRepo = new OffChainDataRepository(_context);
        _service = new AuditChainService(chainRepo, blockRepo, offChainRepo, _rsa);
    }

    [Fact]
    public async Task CreateChainAsync_ShouldPersistChainAndGenesisBlock()
    {
        // Act
        var chain = await _service.CreateChainAsync(
            "test-chain",
            "Test description",
            new { Event = "Genesis" });

        // Assert
        Assert.NotNull(chain);
        Assert.Equal("test-chain", chain.ChainName);
        Assert.Single(chain.Blocks); // Genesis block
        
        // Verify persistence
        var retrieved = await _service.GetChainAsync(chain.ChainId);
        Assert.NotNull(retrieved);
        Assert.Equal(chain.ChainId, retrieved.ChainId);
    }

    [Fact]
    public async Task AddBlockAsync_ShouldPersistNewBlock()
    {
        // Arrange
        var chain = await _service.CreateChainAsync("test-chain", "Test");

        // Act
        var block = await _service.AddBlockAsync(
            chain.ChainId,
            new { Action = "TestAction" },
            new Dictionary<string, string> { { "Type", "Test" } });

        // Assert
        Assert.NotNull(block);
        Assert.Equal(1, block.BlockHeight);
        
        // Verify persistence
        var retrieved = await _service.GetBlockAsync(block.BlockId);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task ValidateChainAsync_ShouldValidatePersistedChain()
    {
        // Arrange
        var chain = await _service.CreateChainAsync("test-chain", "Test");
        await _service.AddBlockAsync(chain.ChainId, new { Data = "Test" });

        // Act
        var result = await _service.ValidateChainAsync(chain.ChainId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.TotalBlocks);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ListChainsAsync_ShouldReturnAllChains()
    {
        // Arrange
        await _service.CreateChainAsync("chain1", "First");
        await _service.CreateChainAsync("chain2", "Second");

        // Act
        var chains = await _service.ListChainsAsync();

        // Assert
        Assert.True(chains.Count >= 2);
    }

    public void Dispose()
    {
        _rsa?.Dispose();
        
        // Clean up database
        if (_context != null)
        {
            var dbPath = _context.Database.GetConnectionString()?.Replace("Data Source=", "");
            _context.Dispose();
            
            if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
