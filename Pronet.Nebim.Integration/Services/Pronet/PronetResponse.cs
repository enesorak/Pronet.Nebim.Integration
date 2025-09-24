using System.Text.Json.Serialization;

namespace Pronet.Nebim.Integration.Services.Pronet;

// Ana Zarf
public class PronetApiResponse
{
    [JsonPropertyName("Result")]
    public PronetResult? Result { get; set; }
}

public class PronetResult
{
    [JsonPropertyName("ResponseText")]
    public string? ResponseText { get; set; }

    [JsonPropertyName("ResponseCode")]
    public string? ResponseCode { get; set; }

    [JsonPropertyName("Data")]
    public PronetData? Data { get; set; }
}

public class PronetData
{
    [JsonPropertyName("StoreStatistics")]
    public List<StoreStatistic>? StoreStatistics { get; set; }
}

public class StoreStatistic
{
    [JsonPropertyName("StoreCode")]
    public string? StoreCode { get; set; }

    [JsonPropertyName("TotalEnterCount")]
    public int TotalEnterCount { get; set; }
}