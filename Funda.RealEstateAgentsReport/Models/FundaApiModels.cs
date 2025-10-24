using System.Text.Json.Serialization;

namespace Funda.RealEstateAgentsReport.Models;

public class FundaApiResponse
{
    [JsonPropertyName("Objects")]
    public List<FundaObject> Objects { get; set; } = new();

    [JsonPropertyName("Paging")]
    public PagingInfo Paging { get; set; } = new();

    [JsonPropertyName("TotaalAantalObjecten")]
    public int TotalObjects { get; set; }
}

public class FundaObject
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("MakelaarId")]
    public int MakelaarId { get; set; }

    [JsonPropertyName("MakelaarNaam")]
    public string MakelaarNaam { get; set; } = string.Empty;

    [JsonPropertyName("Adres")]
    public string Adres { get; set; } = string.Empty;

    [JsonPropertyName("Woonplaats")]
    public string Woonplaats { get; set; } = string.Empty;

    [JsonPropertyName("Koopprijs")]
    public int? Koopprijs { get; set; }

    [JsonPropertyName("HasTuin")]
    public bool HasTuin { get; set; }
}

public class PagingInfo
{
    [JsonPropertyName("AantalPaginas")]
    public int TotalPages { get; set; }

    [JsonPropertyName("HuidigePagina")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("VolgendeUrl")]
    public string? NextUrl { get; set; }

    [JsonPropertyName("VorigeUrl")]
    public string? PreviousUrl { get; set; }
}