namespace Pronet.Nebim.Integration.Services.Nebim;

public class NebimApiService : INebimApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NebimApiService> _logger;
    private readonly ISettingsService _settingsService;
    private string? _sessionizedBaseUrl; // Alınan session'lı URL'i saklamak için

    public NebimApiService(IHttpClientFactory httpClientFactory, ILogger<NebimApiService> logger, ISettingsService settingsService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Nebim V3'e ilk GET isteğini atarak Session'lı URL'i alır ve saklar.
    /// </summary>
    private async Task<bool> InitializeSessionAsync()
    {
        var baseUrl = await _settingsService.GetSettingAsync("ApiUrls:Nebim");
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogError("Nebim API URL ayarı eksik.");
            return false;
        }

        // Zaten bir session alınmışsa tekrar deneme
        if (!string.IsNullOrEmpty(_sessionizedBaseUrl)) return true;

        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);

        try
        {
            // ADIM 1: Ana adrese boş bir GET isteği at.
            var initialResponse = await client.GetAsync(baseUrl);

            // Sunucu 302 (Found) ile yeni URL'i Location header'ında döner.
            if (initialResponse.Headers.Location != null)
            {
                // Gelen URL'in sonundaki "/(S(xxxx))/" kısmını değil, tamamını alıyoruz.
                _sessionizedBaseUrl = initialResponse.Headers.Location.GetLeftPart(UriPartial.Authority) + initialResponse.Headers.Location.PathAndQuery;
                _logger.LogInformation("Nebim'den Session'lı URL alındı.");
                return true;
            }
            
            _logger.LogError("Nebim'den Session'lı URL alınamadı. Yanıt başlıklarında 'Location' bulunamadı.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nebim'e ilk bağlantı (Session URL alma) sırasında hata oluştu.");
            return false;
        }
    }

    public async Task<Guid> ConnectAsync()
    {
        // Önce session'ı başlat
        bool sessionInitialized = await InitializeSessionAsync();
        if (!sessionInitialized || string.IsNullOrEmpty(_sessionizedBaseUrl))
        {
            return Guid.Empty;
        }
        
        // ADIM 2: Connect isteğini, başında session olan URL'e gönder.
        var connectUrl = new Uri(new Uri(_sessionizedBaseUrl), "IntegratorService/Connect").ToString();
        var httpClient = _httpClientFactory.CreateClient();
        var requestBody = new NebimConnectRequest(); // Artık boş

        try
        {
            var httpResponse = await httpClient.PostAsJsonAsync(connectUrl, requestBody);
            if (httpResponse.IsSuccessStatusCode)
            {
                var nebimResponse = await httpResponse.Content.ReadFromJsonAsync<NebimConnectResponse>();

                // CORRECTED: Using the correct property names from the model
                if (nebimResponse != null && nebimResponse.IsSucceeded && nebimResponse.SessionId != Guid.Empty)
                {
                    _logger.LogInformation("Nebim'e başarıyla bağlanıldı (Connect metodu). SessionID: {sessionId}", nebimResponse.SessionId);
                    return nebimResponse.SessionId;
                }
            }
            
            var errorContent = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError("Nebim Connect metodu başarısız oldu. HTTP Status: {StatusCode}, Response: {Response}", httpResponse.StatusCode, errorContent);
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nebim Connect isteği sırasında hata oluştu.");
            return Guid.Empty;
        }
    }
    
    public async Task<bool> PostInOutDataAsync(Guid sessionId, NebimPostRequest data)
    {
        if (string.IsNullOrEmpty(_sessionizedBaseUrl))
        {
             _logger.LogError("Nebim Post isteği yapılamadı. Önce ConnectAsync çağrılmalıdır.");
             return false;
        }

        var postUrl = new Uri(new Uri(_sessionizedBaseUrl), "IntegratorService/Post").ToString();
        var httpClient = _httpClientFactory.CreateClient();

        try
        {
            var httpResponse = await httpClient.PostAsJsonAsync(postUrl, data);
            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Veri Nebim'e başarıyla gönderildi. Mağaza: {StoreCode}", data.Header.StoreCode);
                return true;
            }
            
            var errorContent = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError("Veri Nebim'e gönderilemedi. Status: {StatusCode}, Hata: {Error}", httpResponse.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nebim Post isteği sırasında beklenmedik bir hata oluştu.");
            return false;
        }
    }
}