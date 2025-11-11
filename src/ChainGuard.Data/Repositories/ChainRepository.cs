using ChainGuard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChainGuard.Data.Repositories;

/// <summary>
/// Repository implementation for chain operations.
/// </summary>
public class ChainRepository : IChainRepository
{
    private readonly ChainGuardDbContext _context;

    public ChainRepository(ChainGuardDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ChainEntity?> GetChainByIdAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        return await _context.Chains
            .Include(c => c.Blocks.OrderBy(b => b.BlockHeight))
            .FirstOrDefaultAsync(c => c.ChainId == chainId, cancellationToken);
    }

    public async Task<ChainEntity?> GetChainByNameAsync(string chainName, CancellationToken cancellationToken = default)
    {
        return await _context.Chains
            .Include(c => c.Blocks.OrderBy(b => b.BlockHeight))
            .FirstOrDefaultAsync(c => c.ChainName == chainName, cancellationToken);
    }

    public async Task<List<ChainEntity>> GetChainsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Chains
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChainEntity> CreateChainAsync(ChainEntity chain, CancellationToken cancellationToken = default)
    {
        chain.CreatedAt = DateTime.UtcNow;
        _context.Chains.Add(chain);
        await _context.SaveChangesAsync(cancellationToken);
        return chain;
    }

    public async Task UpdateChainAsync(ChainEntity chain, CancellationToken cancellationToken = default)
    {
        _context.Chains.Update(chain);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteChainAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        var chain = await _context.Chains.FindAsync(new object[] { chainId }, cancellationToken);
        if (chain != null)
        {
            _context.Chains.Remove(chain);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
