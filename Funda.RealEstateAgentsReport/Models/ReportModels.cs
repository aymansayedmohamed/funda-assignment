namespace Funda.RealEstateAgentsReport.Models;

public class RealEstateAgentRanking
{
    public string AgentName { get; set; } = string.Empty;
    public int AgentId { get; set; }
    public int PropertyCount { get; set; }
    public int Rank { get; set; }
}

public class ReportResult
{
    public string CategoryName { get; set; } = string.Empty;
    public string SearchQuery { get; set; } = string.Empty;
    public int TotalObjectsFound { get; set; }
    public int TotalPagesProcessed { get; set; }
    public List<RealEstateAgentRanking> TopRealEstateAgents { get; set; } = new();
    public DateTime ReportDate { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}