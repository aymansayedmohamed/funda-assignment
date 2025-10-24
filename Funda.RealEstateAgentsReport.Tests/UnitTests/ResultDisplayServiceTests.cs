using Moq;
using Microsoft.Extensions.Logging;
using Funda.RealEstateAgentsReport.Services;
using Funda.RealEstateAgentsReport.Models;

namespace Funda.RealEstateAgentsReport.Tests.UnitTests;

public class ResultDisplayServiceTests : IDisposable
{
    private readonly ResultDisplayService _service;
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOut;
    private readonly Mock<ILogger<ResultDisplayService>> _mockLogger;

    public ResultDisplayServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResultDisplayService>>();
        _service = new ResultDisplayService(_mockLogger.Object);
        _stringWriter = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_stringWriter);
    }

    [Fact]
    public void DisplayResults_WithValidData_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new ReportResult
        {
            CategoryName = "Test Category",
            SearchQuery = "/amsterdam/",
            TotalObjectsFound = 100,
            ReportDate = new DateTime(2025, 10, 23, 14, 30, 45, DateTimeKind.Utc),
            ProcessingTime = TimeSpan.FromSeconds(120.5),
            TopRealEstateAgents = new List<RealEstateAgentRanking>
            {
                new() { Rank = 1, AgentName = "Top Agent", PropertyCount = 25 },
                new() { Rank = 2, AgentName = "Second Agent", PropertyCount = 20 }
            }
        };

        // Act
        _service.DisplayResults(result);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("TOP 10 REAL ESTATE AGENTS - TEST CATEGORY", output);
        Assert.Contains("Search Query: /amsterdam/", output);
        Assert.Contains("Total Objects Found: 100", output);
        Assert.Contains("Report Date: 2025-10-23 14:30:45 UTC", output);
        Assert.Contains("Processing Time: 120.50 seconds", output);
        Assert.Contains("Top Agent", output);
        Assert.Contains("Second Agent", output);
        Assert.Contains("25", output);
        Assert.Contains("20", output);
    }

    [Fact]
    public void DisplayResults_WithEmptyAgents_ShouldShowNoAgentsMessage()
    {
        // Arrange
        var result = new ReportResult
        {
            CategoryName = "Empty Test",
            SearchQuery = "/amsterdam/",
            TotalObjectsFound = 0,
            ReportDate = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(10),
            TopRealEstateAgents = new List<RealEstateAgentRanking>()
        };

        // Act
        _service.DisplayResults(result);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("No real estate agents found for this search query.", output);
    }

    [Fact]
    public void DisplayResults_WithLongAgentName_ShouldTruncate()
    {
        // Arrange
        var longName = new string('A', 60); // 60 character name
        var result = new ReportResult
        {
            CategoryName = "Truncation Test",
            SearchQuery = "/amsterdam/",
            TotalObjectsFound = 1,
            ReportDate = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(10),
            TopRealEstateAgents = new List<RealEstateAgentRanking>
            {
                new() { Rank = 1, AgentName = longName, PropertyCount = 5 }
            }
        };

        // Act
        _service.DisplayResults(result);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("...", output); // Should contain truncation indicator
        Assert.DoesNotContain(longName, output); // Should not contain full long name
    }

    [Fact]
    public void DisplayComparison_WithMultipleResults_ShouldShowAllReports()
    {
        // Arrange
        var results = new List<ReportResult>
        {
            new()
            {
                CategoryName = "All Properties",
                SearchQuery = "/amsterdam/",
                TotalObjectsFound = 100,
                ReportDate = DateTime.UtcNow,
                ProcessingTime = TimeSpan.FromSeconds(120),
                TopRealEstateAgents = new List<RealEstateAgentRanking>
                {
                    new() { Rank = 1, AgentName = "Agent A", PropertyCount = 10 }
                }
            },
            new()
            {
                CategoryName = "Garden Properties",
                SearchQuery = "/amsterdam/tuin/",
                TotalObjectsFound = 50,
                ReportDate = DateTime.UtcNow,
                ProcessingTime = TimeSpan.FromSeconds(60),
                TopRealEstateAgents = new List<RealEstateAgentRanking>
                {
                    new() { Rank = 1, AgentName = "Agent B", PropertyCount = 5 }
                }
            }
        };

        // Act
        _service.DisplayComparison(results);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("FUNDA REAL ESTATE AGENTS REPORT", output);
        Assert.Contains("All Properties", output);
        Assert.Contains("Garden Properties", output);
        Assert.Contains("SUMMARY", output);
        Assert.Contains("100 properties, processed in 120.00 seconds", output);
        Assert.Contains("50 properties, processed in 60.00 seconds", output);
        Assert.Contains("Report completed successfully!", output);
    }

    [Fact]
    public void DisplayComparison_WithEmptyResults_ShouldHandleGracefully()
    {
        // Arrange
        var results = new List<ReportResult>();

        // Act
        _service.DisplayComparison(results);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("FUNDA REAL ESTATE AGENTS REPORT", output);
        Assert.Contains("SUMMARY", output);
        Assert.Contains("Report completed successfully!", output);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void DisplayResults_WithInvalidAgentNames_ShouldHandleGracefully(string? agentName)
    {
        // Arrange
        var result = new ReportResult
        {
            CategoryName = "Invalid Name Test",
            SearchQuery = "/amsterdam/",
            TotalObjectsFound = 1,
            ReportDate = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(10),
            TopRealEstateAgents = new List<RealEstateAgentRanking>
            {
                new() { Rank = 1, AgentName = agentName!, PropertyCount = 5 }
            }
        };

        // Act & Assert - Should not throw
        _service.DisplayResults(result);
        
        var output = _stringWriter.ToString();
        Assert.Contains("TOP 10 REAL ESTATE AGENTS", output);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Console.SetOut(_originalOut);
            _stringWriter.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}