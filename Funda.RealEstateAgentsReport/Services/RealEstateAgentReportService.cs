using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Funda.RealEstateAgentsReport.Models;
using Funda.RealEstateAgentsReport.Services.Contracts;

namespace Funda.RealEstateAgentsReport.Services;

public class RealEstateAgentReportService : IRealEstateAgentReportService
{
    private readonly IFundaApiService _fundaApiService;
    private readonly ILogger<RealEstateAgentReportService> _logger;

    public RealEstateAgentReportService(
        IFundaApiService fundaApiService,
        ILogger<RealEstateAgentReportService> logger)
    {
        _fundaApiService = fundaApiService;
        _logger = logger;
    }

    public async Task<ReportResult> GenerateRealEstateAgentReportAsync(string searchQuery, string categoryName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting report generation for: {CategoryName}", categoryName);

        try
        {
            // Fetch all objects for the search query
            var allObjects = await _fundaApiService.GetAllObjectsAsync(searchQuery, cancellationToken);

            // Group by real estate agent and count objects
            var agentCounts = allObjects
                .GroupBy(obj => new { obj.MakelaarId, obj.MakelaarNaam })
                .Select(group => new RealEstateAgentRanking
                {
                    AgentId = group.Key.MakelaarId,
                    AgentName = group.Key.MakelaarNaam,
                    PropertyCount = group.Count()
                })
                .OrderByDescending(m => m.PropertyCount)
                .Take(10)
                .ToList();

            // Assign rankings
            for (int i = 0; i < agentCounts.Count; i++)
            {
                agentCounts[i].Rank = i + 1;
            }

            stopwatch.Stop();

            var result = new ReportResult
            {
                CategoryName = categoryName,
                SearchQuery = searchQuery,
                TotalObjectsFound = allObjects.Count,
                TotalPagesProcessed = CalculatePagesProcessed(allObjects.Count),
                TopRealEstateAgents = agentCounts,
                ReportDate = DateTime.UtcNow,
                ProcessingTime = stopwatch.Elapsed
            };

            _logger.LogInformation("Report generation completed for {CategoryName} in {ProcessingTime:F1} seconds",
                categoryName, stopwatch.Elapsed.TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during report generation for category: {CategoryName}", categoryName);
            throw;
        }
    }

    private static int CalculatePagesProcessed(int totalObjects, int pageSize = 25)
    {
        return (int)Math.Ceiling((double)totalObjects / pageSize);
    }
}