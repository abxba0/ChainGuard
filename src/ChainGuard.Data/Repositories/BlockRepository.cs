using ChainGuard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChainGuard.Data.Repositories;

/// <summary>
/// Repository implementation for block operations.
/// </summary>
public class BlockRepository : IBlockRepository
{
    private readonly ChainGuardDbContext _context;

    public BlockRepository(ChainGuardDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<BlockEntity?> GetBlockByIdAsync(Guid blockId, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Include(b => b.OffChainData)
            .FirstOrDefaultAsync(b => b.BlockId == blockId, cancellationToken);
    }

    public async Task<List<BlockEntity>> GetBlocksByChainIdAsync(Guid chainId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Where(b => b.ChainId == chainId)
            .OrderBy(b => b.BlockHeight)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlockEntity?> GetBlockByHeightAsync(Guid chainId, int blockHeight, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Include(b => b.OffChainData)
            .FirstOrDefaultAsync(b => b.ChainId == chainId && b.BlockHeight == blockHeight, cancellationToken);
    }

    public async Task<BlockEntity?> GetLatestBlockAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Where(b => b.ChainId == chainId)
            .OrderByDescending(b => b.BlockHeight)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BlockEntity> CreateBlockAsync(BlockEntity block, CancellationToken cancellationToken = default)
    {
        block.CreatedAt = DateTime.UtcNow;
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync(cancellationToken);
        return block;
    }

    public async Task<int> GetBlockCountAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .Where(b => b.ChainId == chainId)
            .CountAsync(cancellationToken);
    }
}
