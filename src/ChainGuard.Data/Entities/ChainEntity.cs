using System.ComponentModel.DataAnnotations;

namespace ChainGuard.Data.Entities;

/// <summary>
/// Entity representing a blockchain chain in the database.
/// </summary>
public class ChainEntity
{
    /// <summary>
    /// Unique identifier for the chain.
    /// </summary>
    [Key]
    public Guid ChainId { get; set; }

    /// <summary>
    /// Name of the chain.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ChainName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the chain's purpose.
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the genesis (first) block.
    /// </summary>
    public Guid? GenesisBlockId { get; set; }

    /// <summary>
    /// Reference to the latest block in the chain.
    /// </summary>
    public Guid? LatestBlockId { get; set; }

    /// <summary>
    /// Indicates if the chain is still active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp when the chain was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property for blocks in this chain.
    /// </summary>
    public virtual ICollection<BlockEntity> Blocks { get; set; } = new List<BlockEntity>();
}
