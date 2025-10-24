using Microsoft.Extensions.Logging;
using Funda.RealEstateAgentsReport.Models;
using Funda.RealEstateAgentsReport.Services.Contracts;

namespace Funda.RealEstateAgentsReport.Services;

public class ResultDisplayService : IResultDisplayService
{
    private readonly ILogger<ResultDisplayService> _logger;

    public ResultDisplayService(ILogger<ResultDisplayService> logger)
    {
        _logger = logger;
    }

    public void DisplayResults(ReportResult result)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"TOP 10 REAL ESTATE AGENTS - {result.CategoryName.ToUpper()}");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"Search Query: {result.SearchQuery}");
        Console.WriteLine($"Total Objects Found: {result.TotalObjectsFound:N0}");
        Console.WriteLine($"Report Date: {result.ReportDate:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Processing Time: {result.ProcessingTime.TotalSeconds:F2} seconds");
        Console.WriteLine();

        if (result.TopRealEstateAgents.Any())
        {
            Console.WriteLine($"{"Rank",-6} {"Real Estate Agent Name",-50} {"Properties",-10}");
            Console.WriteLine(new string('-', 70));

            foreach (var agent in result.TopRealEstateAgents)
            {
                Console.WriteLine($"{agent.Rank,-6} {TruncateString(agent.AgentName, 49),-50} {agent.PropertyCount,-10:N0}");
            }
        }
        else
        {
            Console.WriteLine("No real estate agents found for this search query.");
        }

        Console.WriteLine();
    }

    public void DisplayComparison(List<ReportResult> results)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("FUNDA REAL ESTATE AGENTS REPORT");
        Console.WriteLine(new string('=', 100));
        Console.WriteLine($"Report Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();

        foreach (var result in results)
        {
            DisplayResults(result);
        }

        // Display summary
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("SUMMARY");
        Console.WriteLine(new string('=', 100));
        
        foreach (var result in results)
        {
            Console.WriteLine($"{result.CategoryName}: {result.TotalObjectsFound:N0} properties, " +
                             $"processed in {result.ProcessingTime.TotalSeconds:F2} seconds");
        }

        Console.WriteLine();
        Console.WriteLine("Report completed successfully!");
        Console.WriteLine(new string('=', 100));
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}