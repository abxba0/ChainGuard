using System.Security.Cryptography;

namespace ChainGuard.Core.Models;

/// <summary>
/// Represents a blockchain audit chain with validation capabilities.
/// </summary>
public class AuditChain
{
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
    /// RSA instance for signing and verification.
    /// </summary>
    private RSA? _rsa;

    /// <summary>
    /// Creates a new audit chain.
    /// </summary>
    /// <param name="chainName">Name of the chain.</param>
    /// <param name="description">Description of the chain's purpose.</param>
    public AuditChain(string chainName, string description)
    {
        ChainId = Guid.NewGuid();
        ChainName = chainName;
        Description = description;
        Blocks = new List<AuditBlock>();
        IsActive = true;
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
        if (Blocks.Count > 0)
            throw new InvalidOperationException("Genesis block already exists.");

        var genesisBlock = new AuditBlock
        {
            BlockHeight = 0,
            PreviousHash = null,
            PayloadHash = AuditBlock.CalculatePayloadHash(payload),
            PayloadData = payload != null ? System.Text.Json.JsonSerializer.Serialize(payload) : null
        };

        genesisBlock.Metadata["Type"] = "Genesis";
        genesisBlock.FinalizeBlock();

        if (_rsa != null)
        {
            genesisBlock.SignBlock(_rsa);
        }

        Blocks.Add(genesisBlock);
        return genesisBlock;
    }

    /// <summary>
    /// Adds a new block to the chain.
    /// </summary>
    /// <param name="payload">The payload data for the block.</param>
    /// <param name="metadata">Optional metadata for the block.</param>
    /// <returns>The created block.</returns>
    public AuditBlock AddBlock(object payload, Dictionary<string, string>? metadata = null)
    {
        if (Blocks.Count == 0)
            throw new InvalidOperationException("Cannot add block to chain without genesis block. Call CreateGenesisBlock first.");

        var previousBlock = Blocks[^1];
        var newBlock = new AuditBlock
        {
            BlockHeight = previousBlock.BlockHeight + 1,
            PreviousHash = previousBlock.CurrentHash,
            PayloadHash = AuditBlock.CalculatePayloadHash(payload),
            PayloadData = System.Text.Json.JsonSerializer.Serialize(payload)
        };

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                newBlock.Metadata[kvp.Key] = kvp.Value;
            }
        }

        newBlock.FinalizeBlock();

        if (_rsa != null)
        {
            newBlock.SignBlock(_rsa);
        }

        Blocks.Add(newBlock);
        return newBlock;
    }

    /// <summary>
    /// Validates the entire chain's integrity.
    /// </summary>
    /// <returns>Chain validation result.</returns>
    public ChainValidationResult ValidateChain()
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

            // Verify signature if RSA is available
            if (_rsa != null && !string.IsNullOrEmpty(block.Signature))
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

    /// <summary>
    /// Gets the latest block in the chain.
    /// </summary>
    /// <returns>The latest block, or null if chain is empty.</returns>
    public AuditBlock? GetLatestBlock()
    {
        return Blocks.Count > 0 ? Blocks[^1] : null;
    }

    /// <summary>
    /// Gets a block by its height.
    /// </summary>
    /// <param name="height">The block height.</param>
    /// <returns>The block at the specified height, or null if not found.</returns>
    public AuditBlock? GetBlockByHeight(int height)
    {
        return Blocks.FirstOrDefault(b => b.BlockHeight == height);
    }

    /// <summary>
    /// Gets a block by its ID.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <returns>The block with the specified ID, or null if not found.</returns>
    public AuditBlock? GetBlockById(Guid blockId)
    {
        return Blocks.FirstOrDefault(b => b.BlockId == blockId);
    }
}
