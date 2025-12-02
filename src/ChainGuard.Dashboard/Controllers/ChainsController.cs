using ChainGuard.Core.Services;
using ChainGuard.Dashboard.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChainGuard.Dashboard.Controllers;

/// <summary>
/// Controller for managing and visualizing audit chains in the dashboard.
/// </summary>
public class ChainsController : Controller
{
    private readonly IAuditChainService _chainService;
    private readonly ILogger<ChainsController> _logger;

    public ChainsController(IAuditChainService chainService, ILogger<ChainsController> logger)
    {
        _chainService = chainService ?? throw new ArgumentNullException(nameof(chainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all audit chains.
    /// </summary>
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var chains = await _chainService.ListChainsAsync(skip, pageSize);

            var model = new ChainListViewModel
            {
                Chains = chains.Select(c => new ChainSummaryViewModel
                {
                    ChainId = c.ChainId,
                    ChainName = c.ChainName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    BlockCount = c.Blocks.Count
                }).ToList(),
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chains list");
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Shows details of a specific chain.
    /// </summary>
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var chain = await _chainService.GetChainAsync(id);
            if (chain == null)
                return NotFound();

            var model = new ChainDetailsViewModel
            {
                ChainId = chain.ChainId,
                ChainName = chain.ChainName,
                Description = chain.Description,
                IsActive = chain.IsActive,
                Blocks = chain.Blocks.Select(b => new BlockSummaryViewModel
                {
                    BlockId = b.BlockId,
                    BlockHeight = b.BlockHeight,
                    Timestamp = b.Timestamp,
                    CurrentHash = b.CurrentHash,
                    PreviousHash = b.PreviousHash,
                    PayloadHash = b.PayloadHash,
                    HasSignature = !string.IsNullOrEmpty(b.Signature)
                }).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chain details for {ChainId}", id);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Shows detailed information about a specific block.
    /// </summary>
    public async Task<IActionResult> Block(Guid id)
    {
        try
        {
            var block = await _chainService.GetBlockAsync(id);
            if (block == null)
                return NotFound();

            var model = new BlockDetailsViewModel
            {
                BlockId = block.BlockId,
                BlockHeight = block.BlockHeight,
                Timestamp = block.Timestamp,
                CurrentHash = block.CurrentHash,
                PreviousHash = block.PreviousHash,
                PayloadHash = block.PayloadHash,
                Signature = block.Signature,
                Nonce = block.Nonce,
                Metadata = block.Metadata,
                IsHashValid = block.VerifyHash()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading block details for {BlockId}", id);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Validates a chain and shows the results.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Validate(Guid id)
    {
        try
        {
            var result = await _chainService.ValidateChainAsync(id);

            var model = new ChainValidationViewModel
            {
                ChainId = result.ChainId,
                ChainName = result.ChainName,
                IsValid = result.IsValid,
                TotalBlocks = result.TotalBlocks,
                Errors = result.Errors,
                InvalidBlockIds = result.InvalidBlocks,
                ValidatedAt = result.ValidatedAt
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating chain {ChainId}", id);
            return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
