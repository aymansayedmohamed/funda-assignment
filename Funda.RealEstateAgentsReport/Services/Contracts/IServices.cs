using Funda.RealEstateAgentsReport.Models;

namespace Funda.RealEstateAgentsReport.Services.Contracts;

public interface IFundaApiService
{
    Task<FundaApiResponse?> GetObjectsAsync(string searchQuery, int page = 1, CancellationToken cancellationToken = default);
    Task<List<FundaObject>> GetAllObjectsAsync(string searchQuery, CancellationToken cancellationToken = default);
}

public interface IRealEstateAgentReportService
{
    Task<ReportResult> GenerateRealEstateAgentReportAsync(string searchQuery, string categoryName, CancellationToken cancellationToken = default);
}

public interface IResultDisplayService
{
    void DisplayResults(ReportResult result);
    void DisplayComparison(List<ReportResult> results);
}