using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pronet.Nebim.Integration.Data;

namespace Pronet.Nebim.Integration.Services;

public interface ISettingsService
{
    Task<string?> GetSettingAsync(string key);
}

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public SettingsService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Bir ayarı önce veritabanından, bulamazsa appsettings.json'dan okur.
    /// </summary>
    /// <param name="key">Ayarların anahtar adı (Örn: "Pronet:UserName")</param>
    /// <returns>Ayarın değeri</returns>
    public async Task<string?> GetSettingAsync(string key)
    {
        // Önce veritabanını kontrol et
        var dbSetting = await _context.Settings
            .AsNoTracking() // Sadece okuma yapacağımız için performansı artırır
            .FirstOrDefaultAsync(s => s.Key == key);

        if (dbSetting != null && !string.IsNullOrEmpty(dbSetting.Value))
        {
            return dbSetting.Value;
        }

        // Veritabanında yoksa veya değeri boşsa appsettings.json'a bak
        // ':' karakteri IConfiguration'da iç içe JSON objelerini temsil eder.
        return _configuration[key];
    }
}