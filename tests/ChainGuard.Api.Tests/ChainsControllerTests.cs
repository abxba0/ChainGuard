using ChainGuard.Api.Controllers;
using ChainGuard.Core.Models;
using ChainGuard.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainGuard.Api.Tests;

public class ChainsControllerTests
{
    private readonly Mock<IAuditChainService> _mockChainService;
    private readonly Mock<ILogger<ChainsController>> _mockLogger;
    private readonly ChainsController _controller;

    public ChainsControllerTests()
    {
        _mockChainService = new Mock<IAuditChainService>();
        _mockLogger = new Mock<ILogger<ChainsController>>();
        _controller = new ChainsController(_mockChainService.Object, _mockLogger.Object);
    }

    #region CreateChain Tests

    [Fact]
    public async Task CreateChain_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateChainRequest("TestChain", "Test description", null);
        var chain = new AuditChain("TestChain", "Test description");
        chain.CreateGenesisBlock();

        _mockChainService
            .Setup(s => s.CreateChainAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain);

        // Act
        var result = await _controller.CreateChain(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ChainResponse>(createdResult.Value);
        Assert.Equal("TestChain", response.ChainName);
    }

    [Fact]
    public async Task CreateChain_WithNullChainName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateChainRequest(null!, "Test description", null);

        // Act
        var result = await _controller.CreateChain(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateChain_WithEmptyChainName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateChainRequest("", "Test description", null);

        // Act
        var result = await _controller.CreateChain(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateChain_WithChainNameTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var longName = new string('A', 201); // > 200 characters
        var request = new CreateChainRequest(longName, "Test description", null);

        // Act
        var result = await _controller.CreateChain(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateChain_WithNullDescription_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateChainRequest("TestChain", null!, null);

        // Act
        var result = await _controller.CreateChain(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateChain_WithDescriptionTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var longDescription = new string('A', 1001); // > 1000 characters
        var request = new CreateChainRequest("TestChain", longDescription, null);

        // Act
        var result = await _controller.CreateChain(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetChain Tests

    [Fact]
    public async Task GetChain_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var chain = new AuditChain("TestChain", "Test description") { ChainId = chainId };
        chain.CreateGenesisBlock();

        _mockChainService
            .Setup(s => s.GetChainAsync(chainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain);

        // Act
        var result = await _controller.GetChain(chainId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChainResponse>(okResult.Value);
        Assert.Equal(chainId, response.ChainId);
    }

    [Fact]
    public async Task GetChain_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        _mockChainService
            .Setup(s => s.GetChainAsync(chainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditChain?)null);

        // Act
        var result = await _controller.GetChain(chainId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region ListChains Tests

    [Fact]
    public async Task ListChains_WithValidParameters_ShouldReturnOk()
    {
        // Arrange
        var chains = new List<AuditChain>
        {
            new("Chain1", "Description1"),
            new("Chain2", "Description2")
        };
        chains.ForEach(c => c.CreateGenesisBlock());

        _mockChainService
            .Setup(s => s.ListChainsAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chains);

        // Act
        var result = await _controller.ListChains(0, 50, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<ChainResponse>>(okResult.Value);
        Assert.Equal(2, response.Count());
    }

    [Fact]
    public async Task ListChains_WithNegativeSkip_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ListChains(-1, 50, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListChains_WithZeroTake_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ListChains(0, 0, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListChains_WithTakeTooHigh_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ListChains(0, 101, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region ValidateChain Tests

    [Fact]
    public async Task ValidateChain_WithValidChain_ShouldReturnOk()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var validationResult = new ChainValidationResult
        {
            ChainId = chainId,
            ChainName = "TestChain",
            IsValid = true,
            TotalBlocks = 3,
            ValidatedAt = DateTime.UtcNow
        };

        _mockChainService
            .Setup(s => s.ValidateChainAsync(chainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateChain(chainId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ValidationResponse>(okResult.Value);
        Assert.True(response.IsValid);
    }

    [Fact]
    public async Task ValidateChain_WithNonExistentChain_ShouldReturnNotFound()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var validationResult = new ChainValidationResult
        {
            ChainId = chainId,
            IsValid = false,
            Errors = ["Chain not found."]
        };

        _mockChainService
            .Setup(s => s.ValidateChainAsync(chainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateChain(chainId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region AddBlock Tests

    [Fact]
    public async Task AddBlock_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var request = new AddBlockRequest(new { Event = "Test" }, null);
        var block = new AuditBlock
        {
            BlockHeight = 1,
            PayloadHash = AuditBlock.CalculatePayloadHash(request.Payload)
        };
        block.FinalizeBlock();

        _mockChainService
            .Setup(s => s.AddBlockAsync(
                chainId,
                It.IsAny<object>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(block);

        // Act
        var result = await _controller.AddBlock(chainId, request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<BlockResponse>(createdResult.Value);
        Assert.Equal(1, response.BlockHeight);
    }

    [Fact]
    public async Task AddBlock_WithNullPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var request = new AddBlockRequest(null!, null);

        // Act
        var result = await _controller.AddBlock(chainId, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddBlock_WithNonExistentChain_ShouldReturnNotFound()
    {
        // Arrange
        var chainId = Guid.NewGuid();
        var request = new AddBlockRequest(new { Event = "Test" }, null);

        _mockChainService
            .Setup(s => s.AddBlockAsync(
                chainId,
                It.IsAny<object>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Chain not found."));

        // Act
        var result = await _controller.AddBlock(chainId, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region AddOffChainData Tests

    [Fact]
    public async Task AddOffChainData_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var blockId = Guid.NewGuid();
        var request = new AddOffChainDataRequest("UserLogin", new { UserId = 123 }, null);
        var dataId = Guid.NewGuid();

        _mockChainService
            .Setup(s => s.AddOffChainDataAsync(
                blockId,
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataId);

        // Act
        var result = await _controller.AddOffChainData(blockId, request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<OffChainDataResponse>(createdResult.Value);
        Assert.Equal(dataId, response.DataId);
    }

    [Fact]
    public async Task AddOffChainData_WithEmptyDataType_ShouldReturnBadRequest()
    {
        // Arrange
        var blockId = Guid.NewGuid();
        var request = new AddOffChainDataRequest("", new { UserId = 123 }, null);

        // Act
        var result = await _controller.AddOffChainData(blockId, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddOffChainData_WithDataTypeTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var blockId = Guid.NewGuid();
        var longDataType = new string('A', 101); // > 100 characters
        var request = new AddOffChainDataRequest(longDataType, new { UserId = 123 }, null);

        // Act
        var result = await _controller.AddOffChainData(blockId, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddOffChainData_WithNullPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var blockId = Guid.NewGuid();
        var request = new AddOffChainDataRequest("UserLogin", null!, null);

        // Act
        var result = await _controller.AddOffChainData(blockId, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    #endregion

    #region GetBlock Tests

    [Fact]
    public async Task GetBlock_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var blockId = Guid.NewGuid();
        var block = new AuditBlock { BlockId = blockId, BlockHeight = 1 };
        block.FinalizeBlock();

        _mockChainService
            .Setup(s => s.GetBlockAsync(blockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(block);

        // Act
        var result = await _controller.GetBlock(blockId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BlockResponse>(okResult.Value);
        Assert.Equal(blockId, response.BlockId);
    }

    [Fact]
    public async Task GetBlock_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var blockId = Guid.NewGuid();
        _mockChainService
            .Setup(s => s.GetBlockAsync(blockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditBlock?)null);

        // Act
        var result = await _controller.GetBlock(blockId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion
}
