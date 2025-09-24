using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Pronet.Nebim.Integration.Data;
// EKLENDİ

namespace Pronet.Nebim.Integration.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsService> _logger;
    private readonly IDataProtector _protector; // YENİ

    // Constructor'a IDataProtectionProvider enjekte edip ondan bir 'protector' alıyoruz.
    public SettingsService(AppDbContext context, IConfiguration configuration, ILogger<SettingsService> logger, IDataProtectionProvider provider)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        // Bu 'protector', şifreleme/şifre çözme işlemlerini yapacak.
        _protector = provider.CreateProtector("Pronet.Nebim.Integration.Secrets");
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var dbSetting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);

        if (dbSetting != null && !string.IsNullOrEmpty(dbSetting.Value))
        {
            var valueFromDb = dbSetting.Value;
            if (key.Contains("Password", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // YENİ: Şifreyi IDataProtector ile çözüyoruz.
                    return _protector.Unprotect(valueFromDb);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Veritabanından '{key}' ayarı için şifre çözülürken hata oluştu.", key);
                    return null;
                }
            }
            return valueFromDb;
        }
        
        return _configuration[key]; // Yedek olarak appsettings'e bakar.
    }
}