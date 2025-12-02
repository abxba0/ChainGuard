using ChainGuard.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChainGuard.Api.Controllers;

/// <summary>
/// Controller for managing audit chains.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChainsController : ControllerBase
{
    private readonly IAuditChainService _chainService;
    private readonly ILogger<ChainsController> _logger;

    public ChainsController(IAuditChainService chainService, ILogger<ChainsController> logger)
    {
        _chainService = chainService ?? throw new ArgumentNullException(nameof(chainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new audit chain.
    /// </summary>
    /// <param name="request">Chain creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created chain.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChainResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChainResponse>> CreateChain(
        [FromBody] CreateChainRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating new chain: {ChainName}", request.ChainName);

            var chain = await _chainService.CreateChainAsync(
                request.ChainName,
                request.Description,
                request.GenesisPayload,
                cancellationToken);

            var response = new ChainResponse
            {
                ChainId = chain.ChainId,
                ChainName = chain.ChainName,
                Description = chain.Description,
                IsActive = chain.IsActive,
                BlockCount = chain.Blocks.Count
            };

            return CreatedAtAction(nameof(GetChain), new { id = chain.ChainId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chain: {ChainName}", request.ChainName);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a chain by its ID.
    /// </summary>
    /// <param name="id">Chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chain details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChainResponse>> GetChain(Guid id, CancellationToken cancellationToken)
    {
        var chain = await _chainService.GetChainAsync(id, cancellationToken);
        if (chain == null)
            return NotFound(new { error = $"Chain with ID {id} not found." });

        var response = new ChainResponse
        {
            ChainId = chain.ChainId,
            ChainName = chain.ChainName,
            Description = chain.Description,
            IsActive = chain.IsActive,
            BlockCount = chain.Blocks.Count
        };

        return Ok(response);
    }

    /// <summary>
    /// Lists all chains with pagination.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chains.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChainResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChainResponse>>> ListChains(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var chains = await _chainService.ListChainsAsync(skip, take, cancellationToken);
        var response = chains.Select(c => new ChainResponse
        {
            ChainId = c.ChainId,
            ChainName = c.ChainName,
            Description = c.Description,
            IsActive = c.IsActive,
            BlockCount = c.Blocks.Count
        });

        return Ok(response);
    }

    /// <summary>
    /// Validates a chain's integrity.
    /// </summary>
    /// <param name="id">Chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ValidationResponse>> ValidateChain(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating chain: {ChainId}", id);

        var result = await _chainService.ValidateChainAsync(id, cancellationToken);

        var response = new ValidationResponse
        {
            ChainId = result.ChainId,
            ChainName = result.ChainName,
            IsValid = result.IsValid,
            TotalBlocks = result.TotalBlocks,
            Errors = result.Errors,
            InvalidBlockIds = result.InvalidBlocks,
            ValidatedAt = result.ValidatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Adds a new block to a chain.
    /// </summary>
    /// <param name="id">Chain ID.</param>
    /// <param name="request">Block creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created block.</returns>
    [HttpPost("{id:guid}/blocks")]
    [ProducesResponseType(typeof(BlockResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlockResponse>> AddBlock(
        Guid id,
        [FromBody] AddBlockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Adding block to chain: {ChainId}", id);

            var block = await _chainService.AddBlockAsync(id, request.Payload, request.Metadata, cancellationToken);

            var response = new BlockResponse
            {
                BlockId = block.BlockId,
                BlockHeight = block.BlockHeight,
                Timestamp = block.Timestamp,
                PreviousHash = block.PreviousHash,
                CurrentHash = block.CurrentHash,
                PayloadHash = block.PayloadHash,
                Metadata = block.Metadata
            };

            return CreatedAtAction(nameof(GetBlock), new { blockId = block.BlockId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding block to chain: {ChainId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a block by its ID.
    /// </summary>
    /// <param name="blockId">Block ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The block details.</returns>
    [HttpGet("blocks/{blockId:guid}")]
    [ProducesResponseType(typeof(BlockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlockResponse>> GetBlock(Guid blockId, CancellationToken cancellationToken)
    {
        var block = await _chainService.GetBlockAsync(blockId, cancellationToken);
        if (block == null)
            return NotFound(new { error = $"Block with ID {blockId} not found." });

        var response = new BlockResponse
        {
            BlockId = block.BlockId,
            BlockHeight = block.BlockHeight,
            Timestamp = block.Timestamp,
            PreviousHash = block.PreviousHash,
            CurrentHash = block.CurrentHash,
            PayloadHash = block.PayloadHash,
            Metadata = block.Metadata
        };

        return Ok(response);
    }

    /// <summary>
    /// Adds encrypted off-chain data to a block.
    /// </summary>
    /// <param name="blockId">Block ID.</param>
    /// <param name="request">Off-chain data request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created off-chain data ID.</returns>
    [HttpPost("blocks/{blockId:guid}/offchain")]
    [ProducesResponseType(typeof(OffChainDataResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OffChainDataResponse>> AddOffChainData(
        Guid blockId,
        [FromBody] AddOffChainDataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Adding encrypted off-chain data to block: {BlockId}", blockId);

            var dataId = await _chainService.AddOffChainDataAsync(
                blockId,
                request.DataType,
                request.Payload,
                request.Metadata,
                cancellationToken);

            var response = new OffChainDataResponse
            {
                DataId = dataId,
                BlockId = blockId,
                DataType = request.DataType,
                Message = "Off-chain data added and encrypted successfully"
            };

            return CreatedAtAction(nameof(GetOffChainData), new { dataId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding off-chain data to block: {BlockId}", blockId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets and decrypts off-chain data by ID.
    /// </summary>
    /// <param name="dataId">Off-chain data ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted off-chain data.</returns>
    [HttpGet("offchain/{dataId:guid}")]
    [ProducesResponseType(typeof(DecryptedOffChainDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecryptedOffChainDataResponse>> GetOffChainData(
        Guid dataId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving and decrypting off-chain data: {DataId}", dataId);

            var decryptedData = await _chainService.GetOffChainDataAsync(dataId, cancellationToken);
            if (decryptedData == null)
                return NotFound(new { error = $"Off-chain data with ID {dataId} not found." });

            var response = new DecryptedOffChainDataResponse
            {
                DataId = dataId,
                DecryptedPayload = decryptedData
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving off-chain data: {DataId}", dataId);
            return BadRequest(new { error = ex.Message });
        }
    }
}

#region Request/Response Models

public record CreateChainRequest(
    string ChainName,
    string Description,
    object? GenesisPayload = null
);

public record ChainResponse
{
    public Guid ChainId { get; init; }
    public string ChainName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int BlockCount { get; init; }
}

public record AddBlockRequest(
    object Payload,
    Dictionary<string, string>? Metadata = null
);

public record BlockResponse
{
    public Guid BlockId { get; init; }
    public int BlockHeight { get; init; }
    public DateTime Timestamp { get; init; }
    public string? PreviousHash { get; init; }
    public string CurrentHash { get; init; } = string.Empty;
    public string PayloadHash { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public record ValidationResponse
{
    public Guid ChainId { get; init; }
    public string ChainName { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public int TotalBlocks { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<Guid> InvalidBlockIds { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
}

public record AddOffChainDataRequest(
    string DataType,
    object Payload,
    Dictionary<string, string>? Metadata = null
);

public record OffChainDataResponse
{
    public Guid DataId { get; init; }
    public Guid BlockId { get; init; }
    public string DataType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record DecryptedOffChainDataResponse
{
    public Guid DataId { get; init; }
    public string DecryptedPayload { get; init; } = string.Empty;
}

#endregion
