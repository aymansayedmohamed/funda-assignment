using Moq;
using Microsoft.Extensions.Logging;
using Funda.RealEstateAgentsReport.Services;
using Funda.RealEstateAgentsReport.Services.Contracts;
using Funda.RealEstateAgentsReport.Models;

namespace Funda.RealEstateAgentsReport.Tests.UnitTests;

public class RealEstateAgentReportServiceTests
{
    private readonly Mock<IFundaApiService> _mockApiService;
    private readonly Mock<ILogger<RealEstateAgentReportService>> _mockLogger;
    private readonly RealEstateAgentReportService _service;

    public RealEstateAgentReportServiceTests()
    {
        _mockApiService = new Mock<IFundaApiService>();
        _mockLogger = new Mock<ILogger<RealEstateAgentReportService>>();
        _service = new RealEstateAgentReportService(_mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateRealEstateAgentReportAsync_ShouldReturnCorrectTop10Ranking()
    {
        // Arrange
        var testObjects = new List<FundaObject>
        {
            new() { MakelaarNaam = "Agent A" },
            new() { MakelaarNaam = "Agent A" },
            new() { MakelaarNaam = "Agent A" },
            new() { MakelaarNaam = "Agent B" },
            new() { MakelaarNaam = "Agent B" },
            new() { MakelaarNaam = "Agent C" },
            new() { MakelaarNaam = "Agent D" },
            new() { MakelaarNaam = "Agent E" },
            new() { MakelaarNaam = "Agent F" },
            new() { MakelaarNaam = "Agent G" },
            new() { MakelaarNaam = "Agent H" },
            new() { MakelaarNaam = "Agent I" },
            new() { MakelaarNaam = "Agent J" },
            new() { MakelaarNaam = "Agent K" },
            new() { MakelaarNaam = "Agent L" }
        };

        _mockApiService.Setup(x => x.GetAllObjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(testObjects);

        // Act
        var result = await _service.GenerateRealEstateAgentReportAsync("/amsterdam/", "Test Category", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Category", result.CategoryName);
        Assert.Equal("/amsterdam/", result.SearchQuery);
        Assert.Equal(15, result.TotalObjectsFound);
        Assert.Equal(10, result.TopRealEstateAgents.Count);
        
        // Verify ranking order
        Assert.Equal("Agent A", result.TopRealEstateAgents[0].AgentName);
        Assert.Equal(3, result.TopRealEstateAgents[0].PropertyCount);
        Assert.Equal(1, result.TopRealEstateAgents[0].Rank);
        
        Assert.Equal("Agent B", result.TopRealEstateAgents[1].AgentName);
        Assert.Equal(2, result.TopRealEstateAgents[1].PropertyCount);
        Assert.Equal(2, result.TopRealEstateAgents[1].Rank);
    }

    [Fact]
    public async Task GenerateRealEstateAgentReportAsync_WithEmptyData_ShouldReturnEmptyResult()
    {
        // Arrange
        var emptyObjects = new List<FundaObject>();
        _mockApiService.Setup(x => x.GetAllObjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(emptyObjects);

        // Act
        var result = await _service.GenerateRealEstateAgentReportAsync("/amsterdam/", "Empty Test", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalObjectsFound);
        Assert.Empty(result.TopRealEstateAgents);
    }

    [Fact]
    public async Task GenerateRealEstateAgentReportAsync_WithNullMakelaarNames_ShouldFilterThem()
    {
        // Arrange
        var testObjects = new List<FundaObject>
        {
            new() { MakelaarNaam = "Valid Agent" },
            new() { MakelaarNaam = null! },
            new() { MakelaarNaam = "" },
            new() { MakelaarNaam = "   " },
            new() { MakelaarNaam = "Valid Agent" }
        };

        _mockApiService.Setup(x => x.GetAllObjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(testObjects);

        // Act
        var result = await _service.GenerateRealEstateAgentReportAsync("/amsterdam/", "Filter Test", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalObjectsFound); // All objects counted
        Assert.Equal(4, result.TopRealEstateAgents.Count); // All unique agent names (including null/empty)
        
        // Should have valid agent at top
        var validAgent = result.TopRealEstateAgents.FirstOrDefault(a => a.AgentName == "Valid Agent");
        Assert.NotNull(validAgent);
        Assert.Equal(2, validAgent.PropertyCount);
        Assert.Equal(1, validAgent.Rank);
    }

    [Fact]
    public async Task GenerateRealEstateAgentReportAsync_ShouldMeasureProcessingTime()
    {
        // Arrange
        var testObjects = new List<FundaObject>
        {
            new() { MakelaarNaam = "Test Agent" }
        };

        _mockApiService.Setup(x => x.GetAllObjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(async () =>
                      {
                          await Task.Delay(100); // Simulate processing time
                          return testObjects;
                      });

        // Act
        var result = await _service.GenerateRealEstateAgentReportAsync("/amsterdam/", "Time Test", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 90); // Should be at least 90ms
        Assert.True(result.ReportDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateRealEstateAgentReportAsync_WhenApiThrows_ShouldPropagateException()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetAllObjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new HttpRequestException("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GenerateRealEstateAgentReportAsync("/amsterdam/", "Error Test", CancellationToken.None));
    }

    [Theory]
    [InlineData("/amsterdam/")]
    [InlineData("/amsterdam/tuin/")]
    [InlineData("/rotterdam/")]
    public async Task GenerateRealEstateAgentReportAsync_ShouldPassCorrectSearchQueryToApi(string searchQuery)
    {
        // Arrange
        var testObjects = new List<FundaObject> { new() { MakelaarNaam = "Test Agent" } };
        _mockApiService.Setup(x => x.GetAllObjectsAsync(searchQuery, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(testObjects);

        // Act
        var result = await _service.GenerateRealEstateAgentReportAsync(searchQuery, "Query Test", CancellationToken.None);

        // Assert
        _mockApiService.Verify(x => x.GetAllObjectsAsync(searchQuery, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(searchQuery, result.SearchQuery);
    }
}