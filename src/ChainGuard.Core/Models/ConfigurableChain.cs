using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChainGuard.Core.Models;

/// <summary>
/// Represents a configurable blockchain with customizable consensus parameters.
/// </summary>
public class ConfigurableChain
{
    private readonly Lock _lock = new();
    private RSA? _rsa;

    /// <summary>
    /// Unique identifier for this chain.
    /// </summary>
    public Guid ChainId { get; set; }

    /// <summary>
    /// Name of this chain.
    /// </summary>
    public string ChainName { get; set; }

    /// <summary>
    /// Description of this chain's purpose.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// List of all blocks in this chain.
    /// </summary>
    public List<AuditBlock> Blocks { get; private set; }

    /// <summary>
    /// Indicates whether this chain is still active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Consensus configuration for this chain.
    /// </summary>
    public ConsensusConfig Consensus { get; set; }

    /// <summary>
    /// Creation timestamp of the chain.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Statistics about the chain.
    /// </summary>
    public ChainStatistics Statistics { get; private set; }

    /// <summary>
    /// Creates a new configurable chain.
    /// </summary>
    /// <param name="chainName">Name of the chain.</param>
    /// <param name="description">Description of the chain's purpose.</param>
    /// <param name="consensus">Consensus configuration (optional, defaults to None).</param>
    public ConfigurableChain(string chainName, string description, ConsensusConfig? consensus = null)
    {
        if (string.IsNullOrWhiteSpace(chainName))
            throw new ArgumentException("Chain name cannot be null or empty.", nameof(chainName));

        ChainId = Guid.NewGuid();
        ChainName = chainName;
        Description = description ?? string.Empty;
        Blocks = [];
        IsActive = true;
        Consensus = consensus ?? ConsensusConfig.Default;
        CreatedAt = DateTime.UtcNow;
        Statistics = new ChainStatistics();

        // Validate consensus configuration
        var errors = Consensus.Validate();
        if (errors.Count > 0)
            throw new ArgumentException($"Invalid consensus configuration: {string.Join(", ", errors)}");
    }

    /// <summary>
    /// Sets the RSA instance for signing blocks.
    /// </summary>
    /// <param name="rsa">RSA instance with private key.</param>
    public void SetRSA(RSA rsa)
    {
        _rsa = rsa;
    }

    /// <summary>
    /// Creates the genesis (first) block of the chain.
    /// </summary>
    /// <param name="payload">Optional payload for the genesis block.</param>
    /// <returns>The created genesis block.</returns>
    public AuditBlock CreateGenesisBlock(object? payload = null)
    {
        lock (_lock)
        {
            if (Blocks.Count > 0)
                throw new InvalidOperationException("Genesis block already exists.");

            var genesisBlock = new AuditBlock
            {
                BlockHeight = 0,
                PreviousHash = null,
                PayloadHash = AuditBlock.CalculatePayloadHash(payload),
                PayloadData = payload != null ? JsonSerializer.Serialize(payload) : null
            };

            genesisBlock.Metadata["Type"] = "Genesis";
            genesisBlock.Metadata["ConsensusType"] = Consensus.Type.ToString();

            // Apply consensus rules
            ApplyConsensus(genesisBlock);

            // Sign the block if required and RSA is available
            if (Consensus.RequireSignatures && _rsa != null)
            {
                genesisBlock.SignBlock(_rsa);
            }

            Blocks.Add(genesisBlock);
            UpdateStatistics(genesisBlock);
            return genesisBlock;
        }
    }

    /// <summary>
    /// Adds a new block to the chain.
    /// </summary>
    /// <param name="payload">The payload data for the block.</param>
    /// <param name="metadata">Optional metadata for the block.</param>
    /// <returns>The created block.</returns>
    public AuditBlock AddBlock(object payload, Dictionary<string, string>? metadata = null)
    {
        lock (_lock)
        {
            if (Blocks.Count == 0)
                throw new InvalidOperationException("Cannot add block to chain without genesis block. Call CreateGenesisBlock first.");

            // Validate payload size if configured
            if (Consensus.MaxPayloadSizeBytes > 0)
            {
                var payloadJson = JsonSerializer.Serialize(payload);
                if (Encoding.UTF8.GetByteCount(payloadJson) > Consensus.MaxPayloadSizeBytes)
                    throw new InvalidOperationException($"Payload exceeds maximum size of {Consensus.MaxPayloadSizeBytes} bytes.");
            }

            var previousBlock = Blocks[^1];
            var newBlock = new AuditBlock
            {
                BlockHeight = previousBlock.BlockHeight + 1,
                PreviousHash = previousBlock.CurrentHash,
                PayloadHash = AuditBlock.CalculatePayloadHash(payload),
                PayloadData = JsonSerializer.Serialize(payload)
            };

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    newBlock.Metadata[kvp.Key] = kvp.Value;
                }
            }

            // Apply consensus rules
            ApplyConsensus(newBlock);

            // Sign the block if required and RSA is available
            if (Consensus.RequireSignatures && _rsa != null)
            {
                newBlock.SignBlock(_rsa);
            }

            Blocks.Add(newBlock);
            UpdateStatistics(newBlock);
            return newBlock;
        }
    }

    /// <summary>
    /// Applies consensus rules to a block.
    /// </summary>
    /// <param name="block">The block to process.</param>
    private void ApplyConsensus(AuditBlock block)
    {
        switch (Consensus.Type)
        {
            case ConsensusType.ProofOfWork:
                MineBlock(block, Consensus.Difficulty);
                break;

            case ConsensusType.ProofOfAuthority:
                // In PoA, the block is validated against authorized validators
                // The signature verification handles this
                block.FinalizeBlock();
                break;

            case ConsensusType.None:
            default:
                block.FinalizeBlock();
                break;
        }
    }

    /// <summary>
    /// Mines a block using Proof of Work.
    /// </summary>
    /// <param name="block">The block to mine.</param>
    /// <param name="difficulty">Number of leading zeros required.</param>
    private static void MineBlock(AuditBlock block, int difficulty)
    {
        var target = new string('0', difficulty);
        var startTime = DateTime.UtcNow;
        long attempts = 0;

        // Store original nonce and append mining nonce
        var originalNonce = block.Nonce;

        while (true)
        {
            attempts++;
            block.Nonce = $"{originalNonce}:{attempts}";
            block.FinalizeBlock();

            if (block.CurrentHash.StartsWith(target))
            {
                // Successfully mined
                var miningTime = DateTime.UtcNow - startTime;
                block.Metadata["MiningAttempts"] = attempts.ToString();
                block.Metadata["MiningTimeMs"] = miningTime.TotalMilliseconds.ToString("F2");
                break;
            }

            // Prevent infinite loops in testing - cap at 10 million attempts
            if (attempts > 10_000_000)
            {
                throw new InvalidOperationException("Mining failed: exceeded maximum attempts.");
            }
        }
    }

    /// <summary>
    /// Updates chain statistics after adding a block.
    /// </summary>
    /// <param name="block">The newly added block.</param>
    private void UpdateStatistics(AuditBlock block)
    {
        Statistics.TotalBlocks = Blocks.Count;
        Statistics.LastBlockTime = block.Timestamp;
        Statistics.LastBlockHeight = block.BlockHeight;

        if (block.Metadata.TryGetValue("MiningAttempts", out var attempts) &&
            long.TryParse(attempts, out var attemptCount))
        {
            Statistics.TotalMiningAttempts += attemptCount;
        }
    }

    /// <summary>
    /// Validates the entire chain's integrity.
    /// </summary>
    /// <returns>Chain validation result.</returns>
    public ChainValidationResult ValidateChain()
    {
        lock (_lock)
        {
            var result = new ChainValidationResult
            {
                ChainId = ChainId,
                ChainName = ChainName,
                TotalBlocks = Blocks.Count,
                IsValid = true
            };

            if (Blocks.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("Chain has no blocks.");
                return result;
            }

            // Validate genesis block
            var genesisBlock = Blocks[0];
            if (genesisBlock.BlockHeight != 0)
            {
                result.IsValid = false;
                result.Errors.Add("Genesis block height must be 0.");
            }

            if (genesisBlock.PreviousHash != null)
            {
                result.IsValid = false;
                result.Errors.Add("Genesis block should not have a previous hash.");
            }

            // Validate each block
            for (int i = 0; i < Blocks.Count; i++)
            {
                var block = Blocks[i];

                // Verify block hash
                if (!block.VerifyHash())
                {
                    result.IsValid = false;
                    result.Errors.Add($"Block {i} (Height: {block.BlockHeight}) has invalid hash.");
                    result.InvalidBlocks.Add(block.BlockId);
                }

                // Verify PoW if applicable
                if (Consensus.Type == ConsensusType.ProofOfWork)
                {
                    var target = new string('0', Consensus.Difficulty);
                    if (!block.CurrentHash.StartsWith(target))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Block {i} (Height: {block.BlockHeight}) does not meet PoW difficulty requirement.");
                        result.InvalidBlocks.Add(block.BlockId);
                    }
                }

                // Verify signature if RSA is available and signatures are required
                if (Consensus.RequireSignatures && _rsa != null && !string.IsNullOrEmpty(block.Signature))
                {
                    if (!block.VerifySignature(_rsa))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Block {i} (Height: {block.BlockHeight}) has invalid signature.");
                        result.InvalidBlocks.Add(block.BlockId);
                    }
                }

                // Verify chain continuity (except for genesis block)
                if (i > 0)
                {
                    var previousBlock = Blocks[i - 1];
                    if (block.PreviousHash != previousBlock.CurrentHash)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Block {i} (Height: {block.BlockHeight}) has broken chain link.");
                        result.InvalidBlocks.Add(block.BlockId);
                    }

                    if (block.BlockHeight != previousBlock.BlockHeight + 1)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Block {i} (Height: {block.BlockHeight}) has incorrect height.");
                        result.InvalidBlocks.Add(block.BlockId);
                    }

                    // Verify timestamp sequence
                    if (block.Timestamp < previousBlock.Timestamp)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Block {i} (Height: {block.BlockHeight}) has timestamp before previous block.");
                        result.InvalidBlocks.Add(block.BlockId);
                    }
                }
            }

            result.ValidatedAt = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Gets the latest block in the chain.
    /// </summary>
    /// <returns>The latest block, or null if chain is empty.</returns>
    public AuditBlock? GetLatestBlock()
    {
        lock (_lock)
        {
            return Blocks.Count > 0 ? Blocks[^1] : null;
        }
    }

    /// <summary>
    /// Gets a block by its height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block at the specified height, or null if not found.</returns>
    public AuditBlock? GetBlockByHeight(int height)
    {
        lock (_lock)
        {
            return Blocks.FirstOrDefault(b => b.BlockHeight == height);
        }
    }

    /// <summary>
    /// Gets a block by its ID.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <returns>The block with the specified ID, or null if not found.</returns>
    public AuditBlock? GetBlockById(Guid blockId)
    {
        lock (_lock)
        {
            return Blocks.FirstOrDefault(b => b.BlockId == blockId);
        }
    }

    /// <summary>
    /// Gets the current hash rate (hashes per second) based on recent blocks.
    /// </summary>
    /// <returns>Estimated hash rate, or 0 if not applicable.</returns>
    public double GetHashRate()
    {
        if (Consensus.Type != ConsensusType.ProofOfWork || Blocks.Count < 2)
            return 0;

        var recentBlocks = Blocks.TakeLast(Math.Min(10, Blocks.Count)).ToList();
        double totalAttempts = 0;
        double totalTimeMs = 0;

        foreach (var block in recentBlocks)
        {
            if (block.Metadata.TryGetValue("MiningAttempts", out var attempts) &&
                long.TryParse(attempts, out var attemptCount) &&
                block.Metadata.TryGetValue("MiningTimeMs", out var timeMs) &&
                double.TryParse(timeMs, out var time))
            {
                totalAttempts += attemptCount;
                totalTimeMs += time;
            }
        }

        return totalTimeMs > 0 ? (totalAttempts / totalTimeMs) * 1000 : 0;
    }
}

/// <summary>
/// Statistics about a blockchain.
/// </summary>
public class ChainStatistics
{
    /// <summary>
    /// Total number of blocks in the chain.
    /// </summary>
    public int TotalBlocks { get; set; }

    /// <summary>
    /// Timestamp of the last block.
    /// </summary>
    public DateTime LastBlockTime { get; set; }

    /// <summary>
    /// Height of the last block.
    /// </summary>
    public int LastBlockHeight { get; set; }

    /// <summary>
    /// Total mining attempts across all blocks (for PoW chains).
    /// </summary>
    public long TotalMiningAttempts { get; set; }
}
