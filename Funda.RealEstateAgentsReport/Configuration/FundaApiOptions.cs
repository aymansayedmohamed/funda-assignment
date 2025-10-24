namespace Funda.RealEstateAgentsReport.Configuration;

public class FundaApiOptions
{
    public const string SectionName = "FundaApi";
    
    public string ApiKey { get; set; } = "76666a29898f491480386d966b75f949";
    public string BaseUrl { get; set; } = "http://partnerapi.funda.nl/feeds/Aanbod.svc/json";
    public int PageSize { get; set; } = 25;
    public int MaxRequestsPerMinute { get; set; } = 100;
    public int RetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);
}