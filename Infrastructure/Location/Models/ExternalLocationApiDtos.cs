using System.Text.Json.Serialization;

namespace Infrastructure.Location.Models;

internal sealed class ExternalProvinceApiDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

internal sealed class ExternalCityApiDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("state_id")]
    public int StateId { get; set; }
}