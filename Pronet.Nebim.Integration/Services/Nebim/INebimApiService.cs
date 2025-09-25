namespace Pronet.Nebim.Integration.Services.Nebim;

/// <summary>
/// Nebim V3 Integrator servisi ile iletişim kurmak için arayüz.
/// </summary>
public interface INebimApiService
{
    /// <summary>
    /// Nebim V3'e bağlanır ve bir sonraki istekler için kullanılacak Session ID'yi alır.
    /// </summary>
    /// <returns>Başarılı olursa Session ID, başarısız olursa Guid.Empty döner.</returns>
    Task<Guid> ConnectAsync();

    /// <summary>
    /// Hazırlanan veri modelini Nebim V3'e gönderir.
    /// </summary>
    /// <param name="sessionId">ConnectAsync'ten alınan geçerli Session ID.</param>
    /// <param name="data">Gönderilecek veri paketi.</param>
    /// <returns>İşlem başarılı ise true, değilse false döner.</returns>
    Task<bool> PostInOutDataAsync(Guid sessionId, NebimPostRequest data);
}