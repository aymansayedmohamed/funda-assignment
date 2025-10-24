using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Funda.RealEstateAgentsReport.Configuration;
using Funda.RealEstateAgentsReport.Models;
using Funda.RealEstateAgentsReport.Services.Contracts;

namespace Funda.RealEstateAgentsReport.Services;

public class FundaApiService : IFundaApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FundaApiService> _logger;
    private readonly FundaApiOptions _options;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly SemaphoreSlim _rateLimitSemaphore;

    public FundaApiService(
        HttpClient httpClient,
        ILogger<FundaApiService> logger,
        IOptions<FundaApiOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);

        // Configure resilience pipeline with retry
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                Delay = _options.RetryDelay,
                MaxRetryAttempts = _options.RetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("Retry attempt {AttemptNumber} for API call", args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        _httpClient.Timeout = _options.HttpClientTimeout;
    }

    public async Task<FundaApiResponse?> GetObjectsAsync(string searchQuery, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = BuildApiUrl(searchQuery, page);
        
        _logger.LogInformation("Fetching page {Page}: {Url}", page, url);

        // Simple rate limiting
        await _rateLimitSemaphore.WaitAsync(cancellationToken);
        try
        {
            var response = await _resiliencePipeline.ExecuteAsync(async _ =>
            {
                return await _httpClient.GetAsync(url, cancellationToken);
            }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FundaApiResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }

            _logger.LogError("API request failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching data from Funda API");
            throw;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<List<FundaObject>> GetAllObjectsAsync(string searchQuery, CancellationToken cancellationToken = default)
    {
        var allObjects = new List<FundaObject>();
        var page = 1;
        var totalPages = 1;

        do
        {
            var response = await GetObjectsAsync(searchQuery, page, cancellationToken);
            
            if (response == null)
            {
                _logger.LogWarning("Failed to retrieve data for page {Page}", page);
                break;
            }

            allObjects.AddRange(response.Objects);
            totalPages = response.Paging.TotalPages;

            _logger.LogInformation("Processed page {CurrentPage} of {TotalPages}. Total objects so far: {ObjectCount}",
                page, totalPages, allObjects.Count);

            page++;

            // Add a small delay to respect rate limits
            if (page <= totalPages)
            {
                var delayMs = (60 * 1000) / _options.MaxRequestsPerMinute; // Calculate delay based on config
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }

        } while (page <= totalPages && !cancellationToken.IsCancellationRequested);

        _logger.LogInformation("Completed fetching all objects. Total retrieved: {TotalObjects}", allObjects.Count);
        return allObjects;
    }

    private string BuildApiUrl(string searchQuery, int page)
    {
        return $"{_options.BaseUrl}/{_options.ApiKey}/?type=koop&zo={searchQuery}&page={page}&pagesize={_options.PageSize}";
    }
}