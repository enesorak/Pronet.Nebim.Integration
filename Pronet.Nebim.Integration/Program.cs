using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Pronet.Nebim.Integration;
using Pronet.Nebim.Integration.Data;
using Pronet.Nebim.Integration.Data.Models;
using Pronet.Nebim.Integration.Services;
using Pronet.Nebim.Integration.Services.Pronet;

var builder = WebApplication.CreateBuilder(args);


// YENİ: Platform bağımsız şifreleme servisini ekliyoruz.
builder.Services.AddDataProtection();

// --- Servisleri Ekleme (Dependency Injection) ---
// Veritabanı Context'i
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// HTTP İstemci Servisi
builder.Services.AddHttpClient();

// Kendi yazdığımız servisler
builder.Services.AddScoped<IPronetApiService, PronetApiService>();
builder.Services.AddScoped<INebimApiService, NebimApiService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Ana Worker Servisi (Arka planda çalışmaya devam edecek)
builder.Services.AddHostedService<Worker>();

// --- Uygulamayı İnşa Etme ---
var app = builder.Build();

// --- HTTP İstek Hattını (Pipeline) Yapılandırma ---
// index.html gibi varsayılan dosyaları sun
app.UseDefaultFiles();
// wwwroot klasöründeki statik dosyaları (css, js) sun
app.UseStaticFiles();

// --- API Endpoint'lerini Tanımlama ---

// GET: Mevcut tüm ayarları getirir
app.MapGet("/api/settings", async (AppDbContext db) =>
{
    var settings = await db.Settings.ToDictionaryAsync(s => s.Key, s => s.Value ?? "");
    return Results.Ok(settings);
});

app.MapPost("/api/settings", async (AppDbContext db, Dictionary<string, string> newSettings, IDataProtectionProvider provider) =>
{
    var protector = provider.CreateProtector("Pronet.Nebim.Integration.Secrets");
    
    foreach (var setting in newSettings)
    {
        var valueToSave = setting.Value;

        if (setting.Key.Contains("Password", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(valueToSave))
        {
            // YENİ: Şifreyi IDataProtector ile şifreliyoruz.
            valueToSave = protector.Protect(valueToSave);
        }

        var existingSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == setting.Key);
        if (existingSetting != null)
        {
            existingSetting.Value = valueToSave;
        }
        else
        {
            db.Settings.Add(new Setting { Key = setting.Key, Value = valueToSave });
        }
    }
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/api/dashboard/status", async (AppDbContext db) =>
{
    // Her mağazanın en son kaydını al
    var lastSyncs = await db.SyncHistories
        .GroupBy(h => h.DeviceStoreCode)
        .Select(g => g.OrderByDescending(h => h.RunTime).First())
        .ToListAsync();
        
    return Results.Ok(lastSyncs);
});

app.MapGet("/api/devices", async (AppDbContext db) => {
    return Results.Ok(await db.Devices.ToListAsync());
});

app.MapPost("/api/devices", async (AppDbContext db, Device device) => {
    device.CreatedAt = DateTime.Now;
    device.UpdatedAt = DateTime.Now;
    await db.Devices.AddAsync(device);
    await db.SaveChangesAsync();
    return Results.Created($"/api/devices/{device.Id}", device);
});

app.MapPut("/api/devices/{id}", async (AppDbContext db, int id, Device updatedDevice) => {
    var device = await db.Devices.FindAsync(id);
    if (device is null) return Results.NotFound();

    device.IsActive = updatedDevice.IsActive;
    device.NebimOfficeCode = updatedDevice.NebimOfficeCode;
    device.NebimStoreCode = updatedDevice.NebimStoreCode;
    device.MacAddress = updatedDevice.MacAddress;
    device.OpeningTime = updatedDevice.OpeningTime;
    device.ClosingTime = updatedDevice.ClosingTime;
    device.UpdatedAt = DateTime.Now;
    
    await db.SaveChangesAsync();
    return Results.Ok(device);
});

app.MapDelete("/api/devices/{id}", async (AppDbContext db, int id) => {
    var device = await db.Devices.FindAsync(id);
    if (device is null) return Results.NotFound();

    db.Devices.Remove(device);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

 

// --- BAĞLANTI TESTİ API'leri ---

app.MapPost("/api/test/pronet", async (IPronetApiService pronetService, ILogger<Program> logger) =>
{
    logger.LogInformation("Pronet bağlantı testi başlatıldı.");
    try
    {
        bool isSuccess = await pronetService.TestConnectionAsync();

        if (isSuccess)
        {
            logger.LogInformation("Pronet bağlantı testi başarılı.");
            return Results.Ok(new { success = true, message = "Pronet bağlantısı başarılı!" });
        }
        else
        {
            logger.LogWarning("Pronet bağlantı testi başarısız.");
            return Results.Json(new { success = false, message = "Bağlantı başarısız! Bilgileri kontrol edin." }, statusCode: 400);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Pronet bağlantı testi sırasında kritik bir hata oluştu.");
        return Results.Json(new { success = false, message = "Sunucuda bir hata oluştu." }, statusCode: 500);
    }
});

// --- Uygulamayı Başlatma ---
// Bu komut hem web sunucusunu başlatır hem de AddHostedService ile eklenen Worker'ları çalıştırır.
await app.RunAsync();