using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChainGuard.Data.Entities;

/// <summary>
/// Entity representing a block in the blockchain.
/// </summary>
[Table("Blocks")]
[Index(nameof(ChainId), nameof(BlockHeight), IsUnique = true)]
[Index(nameof(ChainId))]
[Index(nameof(BlockHeight))]
[Index(nameof(Timestamp))]
public class BlockEntity
{
    /// <summary>
    /// Unique identifier for the block.
    /// </summary>
    [Key]
    public Guid BlockId { get; set; }

    /// <summary>
    /// Foreign key to the chain this block belongs to.
    /// </summary>
    [Required]
    public Guid ChainId { get; set; }

    /// <summary>
    /// Height/index of the block in the chain (0 for genesis).
    /// </summary>
    public int BlockHeight { get; set; }

    /// <summary>
    /// UTC timestamp when the block was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Hash of the previous block (null for genesis block).
    /// </summary>
    [MaxLength(64)]
    public string? PreviousHash { get; set; }

    /// <summary>
    /// Hash of this block's content.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string CurrentHash { get; set; } = string.Empty;

    /// <summary>
    /// Digital signature of the block.
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Nonce for replay protection.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the payload data.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the block was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the chain.
    /// </summary>
    [ForeignKey(nameof(ChainId))]
    public virtual ChainEntity? Chain { get; set; }

    /// <summary>
    /// Navigation property for off-chain data.
    /// </summary>
    public virtual ICollection<OffChainDataEntity> OffChainData { get; set; } = new List<OffChainDataEntity>();
}
