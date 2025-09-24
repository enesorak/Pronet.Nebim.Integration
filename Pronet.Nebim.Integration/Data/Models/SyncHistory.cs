namespace Pronet.Nebim.Integration.Data.Models;

public class SyncHistory
{
    public int Id { get; set; }
    public DateTime RunTime { get; set; }
    public required string DeviceStoreCode { get; set; }
    public string Status { get; set; } // Örn: "Başarılı", "Hata"
    public string? Message { get; set; } // Hata mesajı veya bilgi
}