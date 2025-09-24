using System.Text.Json.Serialization;

namespace Pronet.Nebim.Integration.Services.Nebim;

// Kişi sayım verisini Nebim'e göndermek için gereken ana model (image001.png'ye göre).
public class NebimPostRequest
{
    [JsonPropertyName("Header")]
    public required NebimHeader Header { get; set; }

    [JsonPropertyName("Lines")]
    public List<NebimLine> Lines { get; set; } = new();
}

public class NebimHeader
{
    [JsonPropertyName("ModelType")]
    public int ModelType { get; set; } = 11;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = "INOUT";

    [JsonPropertyName("OfficeCode")]
    public required string OfficeCode { get; set; }
    
    [JsonPropertyName("StoreCode")]
    public required string StoreCode { get; set; }
    
    [JsonPropertyName("WarehouseCode")]
    public required string WarehouseCode { get; set; }
    
    [JsonPropertyName("DocDate")]
    public required string DocDate { get; set; } // Format: "yyyyMMdd"

    [JsonPropertyName("DocTime")]
    public required string DocTime { get; set; } // Format: "HHmmss"
}

public class NebimLine
{
    [JsonPropertyName("InComingQty")]
    public int InComingQty { get; set; }

    [JsonPropertyName("OutGoingQty")]
    public int OutGoingQty { get; set; } = 0; // Standart olarak 0 gönderiyoruz.

    [JsonPropertyName("LineDescription")]
    public string LineDescription { get; set; } = "Giren-Çıkan Adet";
}