using ChainGuard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChainGuard.Data.Repositories;

/// <summary>
/// Repository implementation for off-chain data operations.
/// </summary>
public class OffChainDataRepository : IOffChainDataRepository
{
    private readonly ChainGuardDbContext _context;

    public OffChainDataRepository(ChainGuardDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<OffChainDataEntity?> GetByIdAsync(Guid dataId, CancellationToken cancellationToken = default)
    {
        return await _context.OffChainData
            .Include(d => d.Block)
            .FirstOrDefaultAsync(d => d.DataId == dataId, cancellationToken);
    }

    public async Task<List<OffChainDataEntity>> GetByBlockIdAsync(Guid blockId, CancellationToken cancellationToken = default)
    {
        return await _context.OffChainData
            .Where(d => d.BlockId == blockId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OffChainDataEntity>> GetByTypeAsync(string dataType, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.OffChainData
            .Where(d => d.DataType == dataType)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<OffChainDataEntity> CreateAsync(OffChainDataEntity data, CancellationToken cancellationToken = default)
    {
        _context.OffChainData.Add(data);
        await _context.SaveChangesAsync(cancellationToken);
        return data;
    }

    public async Task<bool> DeleteAsync(Guid dataId, CancellationToken cancellationToken = default)
    {
        var data = await _context.OffChainData.FindAsync(new object[] { dataId }, cancellationToken);
        if (data == null)
            return false;

        _context.OffChainData.Remove(data);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
