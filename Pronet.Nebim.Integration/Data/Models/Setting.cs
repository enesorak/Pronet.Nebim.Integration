namespace Pronet.Nebim.Integration.Data.Models;

/// <summary>
/// Uygulamanın genel ayarlarını (Pronet, Nebim API bilgileri vb.) anahtar-değer olarak tutar.
/// </summary>
public class Setting
{
    // Primary Key
    public int Id { get; set; }

    public required string Key { get; set; } // Örn: "PronetUserName", "NebimApiUrl"
    public string? Value { get; set; }
}