using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pronet.Nebim.Integration.Services.Nebim;

namespace Pronet.Nebim.Integration.Services;

public interface INebimApiService
{
    // Artık parametre olarak credential almıyor
    Task<Guid> ConnectAsync(); 
    Task<bool> PostInOutDataAsync(Guid sessionId, NebimPostRequest data);
}

public class NebimApiService : INebimApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NebimApiService> _logger;
    private readonly ISettingsService _settingsService; // IConfiguration yerine ISettingsService

    public NebimApiService(IHttpClientFactory httpClientFactory, ILogger<NebimApiService> logger, ISettingsService settingsService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<Guid> ConnectAsync()
    {
        // Ayarları dinamik olarak çekiyoruz
        var apiUrl = await _settingsService.GetSettingAsync("ApiUrls:Nebim");
        var userName = await _settingsService.GetSettingAsync("Credentials:Nebim:UserName");
        var password = await _settingsService.GetSettingAsync("Credentials:Nebim:Password");

        if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            _logger.LogError("Nebim API ayarları (Url, UserName, Password) eksik. Lütfen veritabanını veya appsettings.json'ı kontrol edin.");
            return Guid.Empty;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var requestBody = new NebimConnectRequest { UserName = userName, Password = password };

        try
        {
            var httpResponse = await httpClient.PostAsJsonAsync(apiUrl, requestBody);
            // Kalan kısım aynı...
            if (httpResponse.IsSuccessStatusCode)
            {
                var nebimResponse = await httpResponse.Content.ReadFromJsonAsync<NebimConnectResponse>();
                if (nebimResponse is { IsSucceeded: true })
                {
                    _logger.LogInformation("Nebim'e başarıyla bağlanıldı. SessionID alındı.");
                    return nebimResponse.SessionId;
                }
            }
            _logger.LogError("Nebim'e bağlanılamadı. HTTP Status Kodu: {StatusCode}", httpResponse.StatusCode);
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nebim Connect isteği sırasında beklenmedik bir hata oluştu.");
            return Guid.Empty;
        }
    }

    public async Task<bool> PostInOutDataAsync(Guid sessionId, NebimPostRequest data)
    {
        var apiUrl = await _settingsService.GetSettingAsync("ApiUrls:Nebim");
        if (string.IsNullOrEmpty(apiUrl))
        {
            _logger.LogError("Nebim API URL ayarı eksik.");
            return false;
        }

        if (sessionId == Guid.Empty)
        {
            _logger.LogError("Geçersiz SessionID (Guid.Empty) nedeniyle veri gönderimi atlandı.");
            return false;
        }
        
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionID", sessionId.ToString());

        try
        {
            var httpResponse = await httpClient.PostAsJsonAsync(apiUrl, data);
            // Kalan kısım aynı...
            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Veri Nebim'e başarıyla gönderildi. Mağaza: {StoreCode}", data.Header.StoreCode);
                return true;
            }
            
            var errorContent = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError("Veri Nebim'e gönderilemedi. Status Kodu: {StatusCode}, Hata: {Error}", httpResponse.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nebim Post isteği sırasında beklenmedik bir hata oluştu.");
            return false;
        }
    }
}