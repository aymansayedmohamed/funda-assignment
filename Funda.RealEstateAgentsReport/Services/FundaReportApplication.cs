using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Funda.RealEstateAgentsReport.Services.Contracts;

namespace Funda.RealEstateAgentsReport.Services;

public class FundaReportApplication : IHostedService
{
    private readonly IRealEstateAgentReportService _reportService;
    private readonly IResultDisplayService _displayService;
    private readonly ILogger<FundaReportApplication> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public FundaReportApplication(
        IRealEstateAgentReportService reportService,
        IResultDisplayService displayService,
        ILogger<FundaReportApplication> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _reportService = reportService;
        _displayService = displayService;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Funda Real Estate Agent Report");

        try
        {
            var reportResults = new List<Models.ReportResult>();

            // Report 1: All properties in Amsterdam
            var allPropertiesResult = await _reportService.GenerateRealEstateAgentReportAsync(
                "/amsterdam/", 
                "All Properties in Amsterdam", 
                cancellationToken);
            reportResults.Add(allPropertiesResult);

            // Report 2: Properties with garden in Amsterdam
            var gardenPropertiesResult = await _reportService.GenerateRealEstateAgentReportAsync(
                "/amsterdam/tuin/", 
                "Properties with Garden in Amsterdam", 
                cancellationToken);
            reportResults.Add(gardenPropertiesResult);

            // Display results
            _displayService.DisplayComparison(reportResults);

            _logger.LogInformation("Report completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the report generation");
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Stop the application
            _applicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Funda Real Estate Agent Report Application stopped");
        return Task.CompletedTask;
    }
}