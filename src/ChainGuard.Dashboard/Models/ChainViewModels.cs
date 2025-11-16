namespace ChainGuard.Dashboard.Models;

/// <summary>
/// View model for listing chains.
/// </summary>
public class ChainListViewModel
{
    public List<ChainSummaryViewModel> Chains { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Summary view model for a chain.
/// </summary>
public class ChainSummaryViewModel
{
    public Guid ChainId { get; set; }
    public string ChainName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int BlockCount { get; set; }
}

/// <summary>
/// Detailed view model for a chain.
/// </summary>
public class ChainDetailsViewModel
{
    public Guid ChainId { get; set; }
    public string ChainName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<BlockSummaryViewModel> Blocks { get; set; } = new();
}

/// <summary>
/// Summary view model for a block.
/// </summary>
public class BlockSummaryViewModel
{
    public Guid BlockId { get; set; }
    public int BlockHeight { get; set; }
    public DateTime Timestamp { get; set; }
    public string CurrentHash { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public bool HasSignature { get; set; }
}

/// <summary>
/// Detailed view model for a block.
/// </summary>
public class BlockDetailsViewModel
{
    public Guid BlockId { get; set; }
    public int BlockHeight { get; set; }
    public DateTime Timestamp { get; set; }
    public string CurrentHash { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool IsHashValid { get; set; }
}

/// <summary>
/// View model for chain validation results.
/// </summary>
public class ChainValidationViewModel
{
    public Guid ChainId { get; set; }
    public string ChainName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int TotalBlocks { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> InvalidBlockIds { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}
