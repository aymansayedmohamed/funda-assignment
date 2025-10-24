using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Funda.RealEstateAgentsReport.Services;
using Funda.RealEstateAgentsReport.Configuration;

namespace Funda.RealEstateAgentsReport.Tests.UnitTests;

public class FundaApiServiceTests : IDisposable
{
    private readonly Mock<ILogger<FundaApiService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly FundaApiOptions _options;
    private readonly FundaApiService _service;

    public FundaApiServiceTests()
    {
        _mockLogger = new Mock<ILogger<FundaApiService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        
        _options = new FundaApiOptions
        {
            ApiKey = "test-key",
            BaseUrl = "http://test.api",
            PageSize = 25,
            MaxRequestsPerMinute = 100,
            RetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            HttpClientTimeout = TimeSpan.FromSeconds(30)
        };

        var optionsMock = new Mock<IOptions<FundaApiOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _service = new FundaApiService(_httpClient, _mockLogger.Object, optionsMock.Object);
    }

    [Fact]
    public async Task GetObjectsAsync_WithValidResponse_ShouldReturnParsedData()
    {
        // Arrange
        var jsonResponse = """
        {
            "Objects": [
                {
                    "Id": "12345",
                    "MakelaarNaam": "Test Agent",
                    "Koopprijs": 500000,
                    "Postcode": "1000AA"
                }
            ],
            "Paging": {
                "AantalPaginas": 1,
                "HuidigePagina": 1
            },
            "TotaalAantalObjecten": 1
        }
        """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GetObjectsAsync("/amsterdam/", 1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Objects);
        Assert.Equal("Test Agent", result.Objects[0].MakelaarNaam);
        Assert.Equal(500000, result.Objects[0].Koopprijs);
        Assert.Equal(1, result.TotalObjects);
    }

    [Fact]
    public async Task GetObjectsAsync_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await _service.GetObjectsAsync("/amsterdam/", 1, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetObjectsAsync_ShouldConstructCorrectUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Objects\":[], \"Paging\":{\"AantalPaginas\":0}, \"TotalObjects\":0}")
            });

        // Act
        await _service.GetObjectsAsync("/amsterdam/", 2, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        var expectedUrl = $"{_options.BaseUrl}/{_options.ApiKey}/?type=koop&zo=/amsterdam/&page=2&pagesize={_options.PageSize}";
        Assert.Equal(expectedUrl, capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetAllObjectsAsync_WithMultiplePages_ShouldFetchAllPages()
    {
        // Arrange
        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                callCount++;
                var pageNumber = ExtractPageFromUrl(request.RequestUri?.Query);
                
                if (pageNumber == 1)
                {
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("""
                        {
                            "Objects": [
                                {"Id": "1", "MakelaarNaam": "Agent 1"},
                                {"Id": "2", "MakelaarNaam": "Agent 2"}
                            ],
                            "Paging": {"AantalPaginas": 2, "HuidigePagina": 1},
                            "TotalObjects": 3
                        }
                        """)
                    });
                }
                else
                {
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("""
                        {
                            "Objects": [
                                {"Id": "3", "MakelaarNaam": "Agent 3"}
                            ],
                            "Paging": {"AantalPaginas": 2, "HuidigePagina": 2},
                            "TotalObjects": 3
                        }
                        """)
                    });
                }
            });

        // Act
        var result = await _service.GetAllObjectsAsync("/amsterdam/", CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(2, callCount); // Should have made 2 API calls
        Assert.Contains(result, obj => obj.MakelaarNaam == "Agent 1");
        Assert.Contains(result, obj => obj.MakelaarNaam == "Agent 2");
        Assert.Contains(result, obj => obj.MakelaarNaam == "Agent 3");
    }

    [Fact]
    public async Task GetAllObjectsAsync_WithCancellation_ShouldStopProcessing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
            {
                await Task.Delay(200, ct); // Simulate slow response
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"Objects\":[], \"Paging\":{\"AantalPaginas\":1}, \"TotalObjects\":0}")
                };
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.GetAllObjectsAsync("/amsterdam/", cts.Token));
    }

    private static int ExtractPageFromUrl(string? query)
    {
        if (string.IsNullOrEmpty(query)) return 1;
        
        var pageParam = query.Split('&')
            .FirstOrDefault(p => p.StartsWith("page="));
            
        if (pageParam != null && int.TryParse(pageParam.Split('=')[1], out var page))
            return page;
            
        return 1;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}