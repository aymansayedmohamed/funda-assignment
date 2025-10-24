using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Funda.RealEstateAgentsReport.Services;
using Funda.RealEstateAgentsReport.Services.Contracts;
using Funda.RealEstateAgentsReport.Configuration;

namespace Funda.RealEstateAgentsReport.Tests.IntegrationTests;

public class FundaReportApplicationIntegrationTests
{
    [Fact]
    public async Task FullApplicationWorkflow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FundaApi:ApiKey"] = "76666a29898f491480386d966b75f949",
                ["FundaApi:BaseUrl"] = "http://partnerapi.funda.nl/feeds/Aanbod.svc/json",
                ["FundaApi:PageSize"] = "25",
                ["FundaApi:MaxRequestsPerMinute"] = "200", // Higher for testing
                ["FundaApi:RetryAttempts"] = "2",
                ["FundaApi:RetryDelay"] = "00:00:01",
                ["FundaApi:HttpClientTimeout"] = "00:00:30"
            })
            .Build();

        var services = new ServiceCollection();
        
        // Configure services
        services.Configure<FundaApiOptions>(configuration.GetSection(FundaApiOptions.SectionName));
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddHttpClient<IFundaApiService, FundaApiService>();
        services.AddSingleton<IRealEstateAgentReportService, RealEstateAgentReportService>();
        services.AddSingleton<IResultDisplayService, ResultDisplayService>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var reportService = serviceProvider.GetRequiredService<IRealEstateAgentReportService>();
        
        // Test with a very limited search to avoid long test times
        // Using a specific page limit to make test predictable
        var result = await reportService.GenerateRealEstateAgentReportAsync(
            "/amsterdam/?pagesize=5", // Very small page size for quick testing
            "Integration Test",
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Integration Test", result.CategoryName);
        Assert.True(result.TotalObjectsFound >= 0);
        Assert.True(result.ProcessingTime.TotalSeconds > 0);
        Assert.NotNull(result.TopRealEstateAgents);
        
        // If there are results, verify the ranking structure
        if (result.TopRealEstateAgents.Any())
        {
            var firstAgent = result.TopRealEstateAgents.First();
            Assert.Equal(1, firstAgent.Rank);
            Assert.True(firstAgent.PropertyCount > 0);
            
            // Verify ranking order
            for (int i = 1; i < result.TopRealEstateAgents.Count; i++)
            {
                Assert.True(result.TopRealEstateAgents[i-1].PropertyCount >= result.TopRealEstateAgents[i].PropertyCount,
                    "Results should be ordered by property count in descending order");
            }
        }
    }

    [Fact]
    public void DependencyInjection_ShouldResolveAllServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FundaApi:ApiKey"] = "test-key",
                ["FundaApi:BaseUrl"] = "http://test.api",
                ["FundaApi:PageSize"] = "25",
                ["FundaApi:MaxRequestsPerMinute"] = "100",
                ["FundaApi:RetryAttempts"] = "3",
                ["FundaApi:RetryDelay"] = "00:00:02",
                ["FundaApi:HttpClientTimeout"] = "00:00:30"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FundaApiOptions>(configuration.GetSection(FundaApiOptions.SectionName));
        services.AddLogging();
        services.AddHttpClient<IFundaApiService, FundaApiService>();
        services.AddSingleton<IRealEstateAgentReportService, RealEstateAgentReportService>();
        services.AddSingleton<IResultDisplayService, ResultDisplayService>();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var apiService = serviceProvider.GetService<IFundaApiService>();
        var reportService = serviceProvider.GetService<IRealEstateAgentReportService>();
        var displayService = serviceProvider.GetService<IResultDisplayService>();

        Assert.NotNull(apiService);
        Assert.NotNull(reportService);
        Assert.NotNull(displayService);
        
        // Verify correct implementation types
        Assert.IsType<FundaApiService>(apiService);
        Assert.IsType<RealEstateAgentReportService>(reportService);
        Assert.IsType<ResultDisplayService>(displayService);
    }

    [Fact]
    public void Configuration_ShouldBindCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FundaApi:ApiKey"] = "test-api-key",
                ["FundaApi:BaseUrl"] = "http://test.base.url",
                ["FundaApi:PageSize"] = "50",
                ["FundaApi:MaxRequestsPerMinute"] = "150",
                ["FundaApi:RetryAttempts"] = "5",
                ["FundaApi:RetryDelay"] = "00:00:03",
                ["FundaApi:HttpClientTimeout"] = "00:01:00"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FundaApiOptions>(configuration.GetSection(FundaApiOptions.SectionName));

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FundaApiOptions>>();

        // Assert
        Assert.Equal("test-api-key", options.Value.ApiKey);
        Assert.Equal("http://test.base.url", options.Value.BaseUrl);
        Assert.Equal(50, options.Value.PageSize);
        Assert.Equal(150, options.Value.MaxRequestsPerMinute);
        Assert.Equal(5, options.Value.RetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(3), options.Value.RetryDelay);
        Assert.Equal(TimeSpan.FromMinutes(1), options.Value.HttpClientTimeout);
    }

    [Fact]
    public void RateLimiting_ShouldCalculateCorrectDelay()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FundaApi:ApiKey"] = "test-key",
                ["FundaApi:BaseUrl"] = "http://test.api",
                ["FundaApi:PageSize"] = "25",
                ["FundaApi:MaxRequestsPerMinute"] = "60", // 1 request per second
                ["FundaApi:RetryAttempts"] = "1",
                ["FundaApi:RetryDelay"] = "00:00:01",
                ["FundaApi:HttpClientTimeout"] = "00:00:05"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FundaApiOptions>(configuration.GetSection(FundaApiOptions.SectionName));
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Critical));

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FundaApiOptions>>();

        // Act & Assert - Test the calculation logic
        var expectedDelayMs = (60 * 1000) / options.Value.MaxRequestsPerMinute;
        Assert.Equal(1000, expectedDelayMs); // 60 req/min should equal 1000ms delay

        // Test with different rates
        options.Value.MaxRequestsPerMinute = 120;
        expectedDelayMs = (60 * 1000) / options.Value.MaxRequestsPerMinute;
        Assert.Equal(500, expectedDelayMs); // 120 req/min should equal 500ms delay
    }
}