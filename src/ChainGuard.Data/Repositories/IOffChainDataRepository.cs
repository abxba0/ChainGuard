using ChainGuard.Data.Entities;

namespace ChainGuard.Data.Repositories;

/// <summary>
/// Repository interface for off-chain data operations.
/// </summary>
public interface IOffChainDataRepository
{
    /// <summary>
    /// Gets off-chain data by its ID.
    /// </summary>
    /// <param name="dataId">The data ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The off-chain data entity, or null if not found.</returns>
    Task<OffChainDataEntity?> GetByIdAsync(Guid dataId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all off-chain data for a specific block.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of off-chain data entities.</returns>
    Task<List<OffChainDataEntity>> GetByBlockIdAsync(Guid blockId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets off-chain data by type with pagination.
    /// </summary>
    /// <param name="dataType">The data type (e.g., "UserLogin", "Review").</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of off-chain data entities.</returns>
    Task<List<OffChainDataEntity>> GetByTypeAsync(string dataType, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates new off-chain data.
    /// </summary>
    /// <param name="data">The off-chain data to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created off-chain data entity.</returns>
    Task<OffChainDataEntity> CreateAsync(OffChainDataEntity data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes off-chain data (for GDPR right to erasure).
    /// </summary>
    /// <param name="dataId">The data ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid dataId, CancellationToken cancellationToken = default);
}
