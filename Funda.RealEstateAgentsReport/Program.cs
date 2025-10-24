using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Funda.RealEstateAgentsReport.Configuration;
using Funda.RealEstateAgentsReport.Services;
using Funda.RealEstateAgentsReport.Services.Contracts;

Console.WriteLine("Funda Real Estate Agents Report Tool");
Console.WriteLine("====================================");
Console.WriteLine();

try
{
    var host = CreateHostBuilder(args).Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
    Environment.Exit(1);
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            // Configure options
            services.Configure<FundaApiOptions>(context.Configuration.GetSection(FundaApiOptions.SectionName));

            // Register HTTP client
            services.AddHttpClient<IFundaApiService, FundaApiService>();

            // Register services
            services.AddSingleton<IRealEstateAgentReportService, RealEstateAgentReportService>();
            services.AddSingleton<IResultDisplayService, ResultDisplayService>();
            
            // Register the main application service
            services.AddHostedService<FundaReportApplication>();
        })
        .ConfigureLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
