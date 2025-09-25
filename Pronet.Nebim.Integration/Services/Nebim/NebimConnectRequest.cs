using System.Text.Json.Serialization;

namespace Pronet.Nebim.Integration.Services.Nebim;

// Nebim'den SessionID almak için gönderilecek JSON modelimiz.
// ModelType = 0 sabit.
public class NebimConnectRequest
{
    [JsonPropertyName("ModelType")]
    public int ModelType { get; set; } = 0;

 
}