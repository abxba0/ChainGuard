using ChainGuard.Core.Models;
using ChainGuard.Core.Services;
using ChainGuard.Data.Entities;
using ChainGuard.Data.Repositories;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ChainGuard.Data.Services;

/// <summary>
/// Service implementation for managing audit chains with persistence.
/// </summary>
public class AuditChainService : IAuditChainService
{
    private readonly IChainRepository _chainRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IOffChainDataRepository _offChainDataRepository;
    private readonly RSA _rsa;
    private readonly IEncryptionService? _encryptionService;
    private readonly ILogger<AuditChainService>? _logger;

    public AuditChainService(
        IChainRepository chainRepository,
        IBlockRepository blockRepository,
        IOffChainDataRepository offChainDataRepository,
        RSA rsa,
        IEncryptionService? encryptionService = null,
        ILogger<AuditChainService>? logger = null)
    {
        _chainRepository = chainRepository ?? throw new ArgumentNullException(nameof(chainRepository));
        _blockRepository = blockRepository ?? throw new ArgumentNullException(nameof(blockRepository));
        _offChainDataRepository = offChainDataRepository ?? throw new ArgumentNullException(nameof(offChainDataRepository));
        _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<AuditChain> CreateChainAsync(
        string chainName,
        string description,
        object? genesisPayload = null,
        CancellationToken cancellationToken = default)
    {
        // Create in-memory chain
        var chain = new AuditChain(chainName, description);
        chain.SetRSA(_rsa);
        var genesisBlock = chain.CreateGenesisBlock(genesisPayload);

        // Persist to database
        var chainEntity = new ChainEntity
        {
            ChainId = chain.ChainId,
            ChainName = chain.ChainName,
            Description = chain.Description,
            IsActive = chain.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var blockEntity = MapToBlockEntity(genesisBlock, chain.ChainId);
        await _chainRepository.CreateChainAsync(chainEntity, cancellationToken);
        await _blockRepository.CreateBlockAsync(blockEntity, cancellationToken);

        // Update chain with genesis block reference
        chainEntity.GenesisBlockId = blockEntity.BlockId;
        chainEntity.LatestBlockId = blockEntity.BlockId;
        await _chainRepository.UpdateChainAsync(chainEntity, cancellationToken);

        return chain;
    }

    public async Task<AuditChain?> GetChainAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        var chainEntity = await _chainRepository.GetChainByIdAsync(chainId, cancellationToken);
        if (chainEntity == null)
            return null;

        return MapToAuditChain(chainEntity);
    }

    public async Task<AuditChain?> GetChainByNameAsync(string chainName, CancellationToken cancellationToken = default)
    {
        var chainEntity = await _chainRepository.GetChainByNameAsync(chainName, cancellationToken);
        if (chainEntity == null)
            return null;

        return MapToAuditChain(chainEntity);
    }

    public async Task<AuditBlock> AddBlockAsync(
        Guid chainId,
        object payload,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // Load existing chain
        var chainEntity = await _chainRepository.GetChainByIdAsync(chainId, cancellationToken);
        if (chainEntity == null)
            throw new InvalidOperationException($"Chain with ID {chainId} not found.");

        var chain = MapToAuditChain(chainEntity);
        chain.SetRSA(_rsa);

        // Add block to in-memory chain
        var newBlock = chain.AddBlock(payload, metadata);

        // Persist to database
        var blockEntity = MapToBlockEntity(newBlock, chainId);
        await _blockRepository.CreateBlockAsync(blockEntity, cancellationToken);

        // Update chain's latest block reference
        chainEntity.LatestBlockId = blockEntity.BlockId;
        await _chainRepository.UpdateChainAsync(chainEntity, cancellationToken);

        return newBlock;
    }

    public async Task<ChainValidationResult> ValidateChainAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        var chain = await GetChainAsync(chainId, cancellationToken);
        if (chain == null)
        {
            return new ChainValidationResult
            {
                ChainId = chainId,
                IsValid = false,
                Errors = new List<string> { "Chain not found." }
            };
        }

        chain.SetRSA(_rsa);
        return chain.ValidateChain();
    }

    public async Task<AuditBlock?> GetBlockAsync(Guid blockId, CancellationToken cancellationToken = default)
    {
        var blockEntity = await _blockRepository.GetBlockByIdAsync(blockId, cancellationToken);
        if (blockEntity == null)
            return null;

        return MapToAuditBlock(blockEntity);
    }

    public async Task<List<AuditChain>> ListChainsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var chainEntities = await _chainRepository.GetChainsAsync(skip, take, cancellationToken);
        return chainEntities.Select(MapToAuditChain).ToList();
    }

    private BlockEntity MapToBlockEntity(AuditBlock block, Guid chainId)
    {
        return new BlockEntity
        {
            BlockId = block.BlockId,
            ChainId = chainId,
            BlockHeight = block.BlockHeight,
            Timestamp = block.Timestamp,
            PreviousHash = block.PreviousHash,
            CurrentHash = block.CurrentHash,
            Signature = block.Signature,
            Nonce = block.Nonce,
            PayloadHash = block.PayloadHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    private AuditBlock MapToAuditBlock(BlockEntity entity)
    {
        var block = new AuditBlock
        {
            BlockId = entity.BlockId,
            BlockHeight = entity.BlockHeight,
            Timestamp = entity.Timestamp,
            PreviousHash = entity.PreviousHash,
            Nonce = entity.Nonce,
            PayloadHash = entity.PayloadHash
        };

        // Use reflection to set private properties
        typeof(AuditBlock).GetProperty("CurrentHash")!.SetValue(block, entity.CurrentHash);
        typeof(AuditBlock).GetProperty("Signature")!.SetValue(block, entity.Signature);

        return block;
    }

    private AuditChain MapToAuditChain(ChainEntity entity)
    {
        var chain = new AuditChain(entity.ChainName, entity.Description)
        {
            ChainId = entity.ChainId,
            IsActive = entity.IsActive
        };

        // Add blocks to chain
        var blocks = entity.Blocks
            .OrderBy(b => b.BlockHeight)
            .Select(MapToAuditBlock)
            .ToList();

        // Use reflection to set the private Blocks list
        var blocksProperty = typeof(AuditChain).GetProperty("Blocks")!;
        var blocksList = (List<AuditBlock>)blocksProperty.GetValue(chain)!;
        blocksList.Clear();
        blocksList.AddRange(blocks);

        return chain;
    }

    public async Task<Guid> AddOffChainDataAsync(
        Guid blockId,
        string dataType,
        object payload,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // Verify block exists
        var block = await _blockRepository.GetBlockByIdAsync(blockId, cancellationToken);
        if (block == null)
            throw new InvalidOperationException($"Block with ID {blockId} not found.");

        // Serialize payload
        var payloadJson = JsonSerializer.Serialize(payload);

        // Encrypt if encryption service is available
        string? encryptedPayload = null;
        if (_encryptionService != null)
        {
            try
            {
                encryptedPayload = _encryptionService.Encrypt(payloadJson);
                _logger?.LogInformation("Encrypted off-chain data for block {BlockId}", blockId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to encrypt off-chain data for block {BlockId}", blockId);
                throw;
            }
        }
        else
        {
            _logger?.LogWarning("No encryption service configured. Storing off-chain data unencrypted.");
            encryptedPayload = payloadJson; // Store unencrypted if no service available
        }

        // Create off-chain data entity
        var offChainData = new OffChainDataEntity
        {
            DataId = Guid.NewGuid(),
            BlockId = blockId,
            DataType = dataType,
            EncryptedPayload = encryptedPayload,
            MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow
        };

        await _offChainDataRepository.CreateAsync(offChainData, cancellationToken);
        return offChainData.DataId;
    }

    public async Task<string?> GetOffChainDataAsync(Guid dataId, CancellationToken cancellationToken = default)
    {
        var offChainData = await _offChainDataRepository.GetByIdAsync(dataId, cancellationToken);
        if (offChainData == null)
            return null;

        if (string.IsNullOrEmpty(offChainData.EncryptedPayload))
            return null;

        // Decrypt if encryption service is available
        if (_encryptionService != null)
        {
            try
            {
                var decrypted = _encryptionService.Decrypt(offChainData.EncryptedPayload);
                _logger?.LogInformation("Decrypted off-chain data {DataId}", dataId);
                return decrypted;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to decrypt off-chain data {DataId}", dataId);
                throw;
            }
        }
        else
        {
            _logger?.LogWarning("No encryption service configured. Returning data as-is.");
            return offChainData.EncryptedPayload; // Return as-is if no service available
        }
    }
}
