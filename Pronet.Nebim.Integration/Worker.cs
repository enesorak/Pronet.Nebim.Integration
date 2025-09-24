using Microsoft.EntityFrameworkCore;
using Pronet.Nebim.Integration.Data;
using Pronet.Nebim.Integration.Data.Models;
using Pronet.Nebim.Integration.Services;
using Pronet.Nebim.Integration.Services.Nebim;
using Pronet.Nebim.Integration.Services.Pronet;

namespace Pronet.Nebim.Integration;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ilvi Link Servisi başlatıldı: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            int delayMinutes = 60;
            using (var scope = _serviceProvider.CreateScope())
            {
                var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pronetService = scope.ServiceProvider.GetRequiredService<IPronetApiService>();
                var nebimService = scope.ServiceProvider.GetRequiredService<INebimApiService>();
                
                var frequencySetting = await settingsService.GetSettingAsync("Scheduler:FrequencyMinutes");
                if (int.TryParse(frequencySetting, out var freq) && freq > 0)
                {
                    delayMinutes = freq;
                }

                await ProcessDataTransfer(dbContext, pronetService, nebimService);
            }

            _logger.LogInformation("Döngü tamamlandı. {minutes} dakika sonra yeniden çalışacak.", delayMinutes);
            await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
        }
    }

    private async Task ProcessDataTransfer(AppDbContext dbContext, IPronetApiService pronetService, INebimApiService nebimService)
    {
        _logger.LogInformation("Veri transfer süreci başlıyor: {time}", DateTimeOffset.Now);

        var sessionId = await nebimService.ConnectAsync();
        if (sessionId == Guid.Empty)
        {
            _logger.LogError("Nebim'e bağlanılamadı. Bu döngüdeki işlemler iptal edildi.");
            return;
        }

        var activeDevices = await dbContext.Devices.AsNoTracking().Where(d => d.IsActive).ToListAsync();
        _logger.LogInformation("{count} adet aktif cihaz bulundu.", activeDevices.Count);

        var now = DateTime.Now;
        var currentTime = TimeOnly.FromDateTime(now);

        foreach (var device in activeDevices)
        {
            // YENİ KONTROL: Cihazın çalışma saatleri içinde miyiz?
            if (currentTime < device.OpeningTime || currentTime > device.ClosingTime)
            {
                _logger.LogInformation("Cihaz '{StoreCode}' ({OpeningTime}-{ClosingTime}) şu anda kapalı. Veri çekme işlemi atlandı.", 
                    device.NebimStoreCode, device.OpeningTime, device.ClosingTime);
                continue; // Bu cihazı atla ve bir sonrakine geç
            }

            // Bir önceki saatin verisini çekmek için başlangıç ve bitiş zamanlarını ayarla
            var startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(-1);
            var endTime = startTime.AddHours(1).AddSeconds(-1);

            _logger.LogInformation("Cihaz '{StoreCode}' için {startTime} - {endTime} arası veri çekiliyor.", device.NebimStoreCode, startTime, endTime);
            
            string status = "Başarılı";
            string message = "Veri başarıyla aktarıldı.";
            
            try
            {
                var pronetData = await pronetService.GetStatisticsAsync(device, startTime, endTime);

                // ESKİ ve HATALI KOD:
                // if (pronetData?.StoreStatistics is null || !pronetData.StoreStatistics.Any())
            
                // YENİ ve DOĞRU KOD:
                if (pronetData?.Result?.Data?.StoreStatistics is null || !pronetData.Result.Data.StoreStatistics.Any())
                {
                    _logger.LogWarning("Cihaz '{StoreCode}' için Pronet'ten veri alınamadı veya veri boş.", device.NebimStoreCode);
                    // ... Hata durumunu DB'ye kaydetme ...
                    continue; 
                }
                foreach (var stats in pronetData.Result.Data.StoreStatistics)
                {
                    // 4. Gelen Veriyi Nebim Modeline Dönüştür
                    var nebimRequest = new NebimPostRequest
                    {
                        Header = new NebimHeader
                        {
                            OfficeCode = device.NebimOfficeCode,
                            StoreCode = device.NebimStoreCode,
                            WarehouseCode = device.NebimStoreCode, 
                            DocDate = startTime.ToString("yyyyMMdd"),
                            DocTime = startTime.ToString("HHmmss")
                        },
                        Lines = new List<NebimLine>
                        {
                            new NebimLine { InComingQty = stats.TotalEnterCount }
                        }
                    };
                    
                    bool success = await nebimService.PostInOutDataAsync(sessionId, nebimRequest);
                    if (!success)
                    {
                        throw new Exception("Veri Nebim'e gönderilemedi.");
                    }
                }
            }
            catch (Exception ex)
            {
                status = "Hata";
                message = ex.Message;
                _logger.LogError(ex, "Cihaz '{StoreCode}' için veri aktarımında hata.", device.NebimStoreCode);
            }
            
            var history = new SyncHistory
            {
                RunTime = DateTime.Now,
                DeviceStoreCode = device.NebimStoreCode,
                Status = status,
                Message = message
            };
            dbContext.SyncHistories.Add(history);
            await dbContext.SaveChangesAsync();
        }
    }
}