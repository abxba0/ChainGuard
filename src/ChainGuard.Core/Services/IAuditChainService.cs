using ChainGuard.Core.Models;

namespace ChainGuard.Core.Services;

/// <summary>
/// Service interface for managing audit chains.
/// </summary>
public interface IAuditChainService
{
    /// <summary>
    /// Creates a new audit chain with a genesis block.
    /// </summary>
    /// <param name="chainName">Name of the chain.</param>
    /// <param name="description">Description of the chain's purpose.</param>
    /// <param name="genesisPayload">Optional payload for the genesis block.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created chain.</returns>
    Task<AuditChain> CreateChainAsync(string chainName, string description, object? genesisPayload = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chain by its ID.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain, or null if not found.</returns>
    Task<AuditChain?> GetChainAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chain by its name.
    /// </summary>
    /// <param name="chainName">The chain name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain, or null if not found.</returns>
    Task<AuditChain?> GetChainByNameAsync(string chainName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new block to an existing chain.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="payload">The payload data for the block.</param>
    /// <param name="metadata">Optional metadata for the block.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created block.</returns>
    Task<AuditBlock> AddBlockAsync(Guid chainId, object payload, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of an entire chain.
    /// </summary>
    /// <param name="chainId">The chain ID to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ChainValidationResult> ValidateChainAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a block by its ID.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block, or null if not found.</returns>
    Task<AuditBlock?> GetBlockAsync(Guid blockId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all chains with pagination.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chains.</returns>
    Task<List<AuditChain>> ListChainsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds off-chain data to a block with encryption.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <param name="dataType">Type of data (e.g., "UserLogin", "Review").</param>
    /// <param name="payload">The payload data to encrypt and store.</param>
    /// <param name="metadata">Optional searchable metadata (non-sensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The off-chain data ID.</returns>
    Task<Guid> AddOffChainDataAsync(Guid blockId, string dataType, object payload, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets and decrypts off-chain data by ID.
    /// </summary>
    /// <param name="dataId">The off-chain data ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted payload data, or null if not found.</returns>
    Task<string?> GetOffChainDataAsync(Guid dataId, CancellationToken cancellationToken = default);
}
