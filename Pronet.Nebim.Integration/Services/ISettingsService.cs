namespace Pronet.Nebim.Integration.Services;

/// <summary>
/// Uygulama ayarlarını yöneten servis için arayüz.
/// Ayarları veritabanından veya yapılandırma dosyalarından okur.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Belirtilen anahtara sahip ayarın değerini getirir.
    /// Hassas verileri (şifreler) otomatik olarak çözer.
    /// </summary>
    /// <param name="key">Aranan ayarın anahtarı (Örn: "Credentials:Pronet:Password")</param>
    /// <returns>Ayarın çözülmüş değeri veya bulunamazsa null.</returns>
    Task<string?> GetSettingAsync(string key);
}