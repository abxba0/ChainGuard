using ChainGuard.Data.Entities;

namespace ChainGuard.Data.Repositories;

/// <summary>
/// Repository interface for block operations.
/// </summary>
public interface IBlockRepository
{
    /// <summary>
    /// Gets a block by its ID.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block entity, or null if not found.</returns>
    Task<BlockEntity?> GetBlockByIdAsync(Guid blockId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets blocks for a specific chain with pagination.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of block entities.</returns>
    Task<List<BlockEntity>> GetBlocksByChainIdAsync(Guid chainId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a block by chain ID and block height.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="blockHeight">The block height.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block entity, or null if not found.</returns>
    Task<BlockEntity?> GetBlockByHeightAsync(Guid chainId, int blockHeight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest block for a chain.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest block entity, or null if chain has no blocks.</returns>
    Task<BlockEntity?> GetLatestBlockAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new block.
    /// </summary>
    /// <param name="block">The block to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created block entity.</returns>
    Task<BlockEntity> CreateBlockAsync(BlockEntity block, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of blocks in a chain.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of blocks.</returns>
    Task<int> GetBlockCountAsync(Guid chainId, CancellationToken cancellationToken = default);
}
