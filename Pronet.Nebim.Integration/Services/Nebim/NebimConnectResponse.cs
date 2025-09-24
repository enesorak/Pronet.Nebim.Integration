using System.Text.Json.Serialization;

namespace Pronet.Nebim.Integration.Services.Nebim;

// Nebim'den SessionID alındığında dönen yanıt.
public class NebimConnectResponse
{
    [JsonPropertyName("SessionID")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("IsSucceeded")]
    public bool IsSucceeded { get; set; }
}