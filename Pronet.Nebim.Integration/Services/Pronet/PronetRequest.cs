using System.Text.Json.Serialization;

namespace Pronet.Nebim.Integration.Services.Pronet;

// Pronet API'sine gönderilecek JSON body'sini temsil eden C# sınıfı.
// Alan adlarını JSON ile eşleştirmek için JsonPropertyName attribute'u kullanıyoruz.
public class PronetRequest
{
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }

    [JsonPropertyName("passWord")]
    public required string PassWord { get; set; }

    [JsonPropertyName("startTime")]
    public required string StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public required string EndTime { get; set; }

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = "0"; // 0: Saatlik

    [JsonPropertyName("storeCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StoreCode { get; set; }

    [JsonPropertyName("macAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MacAddress { get; set; }

    [JsonPropertyName("ip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ip { get; set; }

    [JsonPropertyName("port")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Port { get; set; }
}