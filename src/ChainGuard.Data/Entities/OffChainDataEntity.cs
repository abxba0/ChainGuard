using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChainGuard.Data.Entities;

/// <summary>
/// Entity representing sensitive off-chain data that is referenced by blocks.
/// </summary>
[Table("OffChainData")]
[Index(nameof(BlockId))]
[Index(nameof(DataType))]
[Index(nameof(CreatedAt))]
public class OffChainDataEntity
{
    /// <summary>
    /// Unique identifier for this data record.
    /// </summary>
    [Key]
    public Guid DataId { get; set; }

    /// <summary>
    /// Foreign key to the block this data belongs to.
    /// </summary>
    [Required]
    public Guid BlockId { get; set; }

    /// <summary>
    /// Type of data (e.g., "UserRegistration", "Review", "Login").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted payload data.
    /// </summary>
    public string? EncryptedPayload { get; set; }

    /// <summary>
    /// Non-sensitive metadata in JSON format (searchable fields).
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Timestamp when the data was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the block.
    /// </summary>
    [ForeignKey(nameof(BlockId))]
    public virtual BlockEntity? Block { get; set; }
}
