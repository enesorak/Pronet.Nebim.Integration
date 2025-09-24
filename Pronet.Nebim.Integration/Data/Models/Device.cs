namespace Pronet.Nebim.Integration.Data.Models;

public class Device
{
    public int Id { get; set; }
    
    // Sizin istediğiniz yeni alanlar
    public bool IsActive { get; set; }
    public required string NebimOfficeCode { get; set; }
    public required string NebimStoreCode { get; set; } // Bu, Nebim'deki mağaza kodudur.
    public string? PronetStoreCode { get; set; } // Bu, Pronet API'si için opsiyonel koddur.
    public string? MacAddress { get; set; }
    public TimeOnly OpeningTime { get; set; } // Mağaza açılış saati (örn: 09:00)
    public TimeOnly ClosingTime { get; set; } // Mağaza kapanış saati (örn: 22:00)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Eski, artık gerekmeyen alanlar (opsiyonel olarak tutulabilir veya silinebilir)
    // public string? IpAddress { get; set; }
    // public string? Port { get; set; }
    // public required string NebimWarehouseCode { get; set; }
    // public string? Description { get; set; }
}