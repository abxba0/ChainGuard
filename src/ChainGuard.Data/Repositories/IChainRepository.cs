using ChainGuard.Data.Entities;

namespace ChainGuard.Data.Repositories;

/// <summary>
/// Repository interface for chain operations.
/// </summary>
public interface IChainRepository
{
    /// <summary>
    /// Gets a chain by its ID including all blocks.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain entity, or null if not found.</returns>
    Task<ChainEntity?> GetChainByIdAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chain by its name.
    /// </summary>
    /// <param name="chainName">The chain name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain entity, or null if not found.</returns>
    Task<ChainEntity?> GetChainByNameAsync(string chainName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all chains with pagination.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chain entities.</returns>
    Task<List<ChainEntity>> GetChainsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new chain.
    /// </summary>
    /// <param name="chain">The chain to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created chain entity.</returns>
    Task<ChainEntity> CreateChainAsync(ChainEntity chain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing chain.
    /// </summary>
    /// <param name="chain">The chain to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateChainAsync(ChainEntity chain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a chain.
    /// </summary>
    /// <param name="chainId">The chain ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteChainAsync(Guid chainId, CancellationToken cancellationToken = default);
}
