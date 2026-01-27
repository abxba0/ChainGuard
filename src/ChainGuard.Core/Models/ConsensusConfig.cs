using System.Text.Json.Serialization;

namespace ChainGuard.Core.Models;

/// <summary>
/// Defines the consensus mechanism type.
/// </summary>
public enum ConsensusType
{
    /// <summary>
    /// No consensus required - blocks are immediately accepted (default for audit chains).
    /// </summary>
    None = 0,

    /// <summary>
    /// Proof of Work - requires mining with difficulty target.
    /// </summary>
    ProofOfWork = 1,

    /// <summary>
    /// Proof of Authority - only authorized nodes can create blocks.
    /// </summary>
    ProofOfAuthority = 2
}

/// <summary>
/// Configuration for blockchain consensus parameters.
/// </summary>
public class ConsensusConfig
{
    /// <summary>
    /// The type of consensus mechanism to use.
    /// </summary>
    public ConsensusType Type { get; set; } = ConsensusType.None;

    /// <summary>
    /// Difficulty level for Proof of Work (number of leading zeros required in hash).
    /// Valid range: 1-6 for practical purposes.
    /// </summary>
    public int Difficulty { get; set; } = 2;

    /// <summary>
    /// Block time target in seconds (for difficulty adjustment).
    /// </summary>
    public int TargetBlockTimeSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of transactions per block (0 = unlimited).
    /// </summary>
    public int MaxTransactionsPerBlock { get; set; } = 0;

    /// <summary>
    /// Maximum payload size in bytes (0 = unlimited).
    /// </summary>
    public int MaxPayloadSizeBytes { get; set; } = 0;

    /// <summary>
    /// Whether to require digital signatures on blocks.
    /// </summary>
    public bool RequireSignatures { get; set; } = true;

    /// <summary>
    /// Number of blocks between difficulty adjustments (0 = no adjustment).
    /// </summary>
    public int DifficultyAdjustmentInterval { get; set; } = 0;

    /// <summary>
    /// List of authorized public keys for Proof of Authority consensus.
    /// </summary>
    public List<string> AuthorizedValidators { get; set; } = [];

    /// <summary>
    /// Creates a default configuration for audit chains (no consensus).
    /// </summary>
    public static ConsensusConfig Default => new()
    {
        Type = ConsensusType.None,
        RequireSignatures = true
    };

    /// <summary>
    /// Creates a configuration for Proof of Work consensus.
    /// </summary>
    /// <param name="difficulty">Number of leading zeros required (1-6).</param>
    /// <param name="targetBlockTimeSeconds">Target time between blocks.</param>
    public static ConsensusConfig ProofOfWork(int difficulty = 2, int targetBlockTimeSeconds = 10) => new()
    {
        Type = ConsensusType.ProofOfWork,
        Difficulty = Math.Clamp(difficulty, 1, 6),
        TargetBlockTimeSeconds = targetBlockTimeSeconds,
        RequireSignatures = true
    };

    /// <summary>
    /// Creates a configuration for Proof of Authority consensus.
    /// </summary>
    /// <param name="authorizedValidators">List of authorized validator public keys.</param>
    public static ConsensusConfig ProofOfAuthority(IEnumerable<string> authorizedValidators) => new()
    {
        Type = ConsensusType.ProofOfAuthority,
        AuthorizedValidators = [.. authorizedValidators],
        RequireSignatures = true
    };

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Type == ConsensusType.ProofOfWork)
        {
            if (Difficulty < 1 || Difficulty > 6)
                errors.Add("Difficulty must be between 1 and 6 for Proof of Work.");

            if (TargetBlockTimeSeconds < 1)
                errors.Add("Target block time must be at least 1 second.");
        }

        if (Type == ConsensusType.ProofOfAuthority)
        {
            if (AuthorizedValidators.Count == 0)
                errors.Add("At least one authorized validator is required for Proof of Authority.");
        }

        if (MaxTransactionsPerBlock < 0)
            errors.Add("Maximum transactions per block cannot be negative.");

        if (MaxPayloadSizeBytes < 0)
            errors.Add("Maximum payload size cannot be negative.");

        return errors;
    }
}
