namespace Pronet.Nebim.Integration.Data.Models;

/// <summary>
/// Uygulamanın lisans bilgilerini tutar.
/// </summary>
public class License
{
    // Primary Key
    public int Id { get; set; }

    public required string LicenseKey { get; set; } // Şifrelenmiş lisans anahtarı
    
    // Lisans anahtarından çözülen ve kolay erişim için saklanan bilgiler
    public DateTime UpdatesValidUntil { get; set; } // Güncellemelerin geçerli olduğu son tarih
    public string? LicensedTo { get; set; } // Lisansın kime ait olduğu
}