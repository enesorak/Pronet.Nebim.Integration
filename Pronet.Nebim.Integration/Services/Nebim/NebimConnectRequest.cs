using System.Text.Json.Serialization;

namespace Pronet.Nebim.Integration.Services.Nebim;

// Nebim'den SessionID almak için gönderilecek JSON modelimiz.
// ModelType = 0 sabit.
public class NebimConnectRequest
{
    [JsonPropertyName("ModelType")]
    public int ModelType { get; set; } = 0;

    [JsonPropertyName("UserName")]
    public required string UserName { get; set; }

    [JsonPropertyName("Password")]
    public required string Password { get; set; }

    [JsonPropertyName("ClientIP")]
    public string ClientIP { get; set; } = "127.0.0.1";
}