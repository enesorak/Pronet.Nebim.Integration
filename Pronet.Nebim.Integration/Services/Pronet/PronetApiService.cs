using Pronet.Nebim.Integration.Data.Models;

// Modellerin olduğu namespace

namespace Pronet.Nebim.Integration.Services.Pronet;

public interface IPronetApiService
{
    // Dönüş tipini PronetApiResponse? olarak güncelliyoruz
    Task<PronetApiResponse?> GetStatisticsAsync(Device device, DateTime startTime, DateTime endTime);
    
    Task<bool> TestConnectionAsync();
}

public class PronetApiService(
    IHttpClientFactory httpClientFactory,
    ILogger<PronetApiService> logger,
    ISettingsService settingsService)
    : IPronetApiService
{
    public async Task<PronetApiResponse?> GetStatisticsAsync(Device device, DateTime startTime, DateTime endTime)
    {
        var apiUrl = await settingsService.GetSettingAsync("ApiUrls:Pronet");
        var userName = await settingsService.GetSettingAsync("Credentials:Pronet:UserName");
        var password = await settingsService.GetSettingAsync("Credentials:Pronet:Password");

        if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            logger.LogError("Pronet API ayarları eksik.");
            return null;
        }

        var requestBody = new PronetRequest
        {
            UserName = userName,
            PassWord = password,
            StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Interval = "0",
            StoreCode = device.PronetStoreCode,
            MacAddress = device.MacAddress
        };
        
        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var httpResponse = await httpClient.PostAsJsonAsync(apiUrl, requestBody);

            if (httpResponse.IsSuccessStatusCode)
            {
                // Değişken tipini PronetApiResponse olarak güncelliyoruz
                var apiResponse = await httpResponse.Content.ReadFromJsonAsync<PronetApiResponse>();
                
                if (apiResponse?.Result?.ResponseText != "Başarılı")
                {
                    logger.LogError("Pronet API Hata Döndürdü: {HataMesaji}", apiResponse?.Result?.ResponseText);
                    return null;
                }
                
                return apiResponse;
            }
            else
            {
                 logger.LogError("Pronet API'ye ulaşılamadı. HTTP Status Kodu: {StatusCode}", httpResponse.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pronet GetStatistics isteği sırasında hata.");
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        var apiUrl = await settingsService.GetSettingAsync("ApiUrls:Pronet");
        var userName = await settingsService.GetSettingAsync("Credentials:Pronet:UserName");
        var password = await settingsService.GetSettingAsync("Credentials:Pronet:Password");
        if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return false;

        var httpClient = httpClientFactory.CreateClient();
        
        var requestBody = new PronetRequest
        {
            UserName = userName,
            PassWord = password,
            StartTime = DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss"),
            Interval = "0"
        };

        try
        {
            var httpResponse = await httpClient.PostAsJsonAsync(apiUrl, requestBody);
            if (!httpResponse.IsSuccessStatusCode) return false;

            // Değişken tipini PronetApiResponse olarak güncelliyoruz
            var apiResponse = await httpResponse.Content.ReadFromJsonAsync<PronetApiResponse>();
            return apiResponse?.Result?.ResponseText == "Başarılı";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pronet bağlantı testi sırasında hata.");
            return false;
        }
    }
}