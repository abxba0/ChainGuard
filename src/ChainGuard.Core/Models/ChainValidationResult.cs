namespace ChainGuard.Core.Models;

/// <summary>
/// Contains the results of a chain validation operation.
/// </summary>
public class ChainValidationResult
{
    /// <summary>
    /// The ID of the validated chain.
    /// </summary>
    public Guid ChainId { get; set; }

    /// <summary>
    /// The name of the validated chain.
    /// </summary>
    public string ChainName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the chain is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Total number of blocks in the chain.
    /// </summary>
    public int TotalBlocks { get; set; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<string> Errors { get; set; }

    /// <summary>
    /// List of invalid block IDs.
    /// </summary>
    public List<Guid> InvalidBlocks { get; set; }

    /// <summary>
    /// Timestamp when validation was performed.
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    /// <summary>
    /// Creates a new validation result.
    /// </summary>
    public ChainValidationResult()
    {
        Errors = new List<string>();
        InvalidBlocks = new List<Guid>();
    }
}
