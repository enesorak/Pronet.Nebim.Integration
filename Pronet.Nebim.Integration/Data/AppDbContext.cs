using Microsoft.EntityFrameworkCore;
using Pronet.Nebim.Integration.Data.Models;

namespace Pronet.Nebim.Integration.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Veritabanı tablolarımızı temsil eden DbSet özellikleri
    public DbSet<Setting> Settings { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<License> Licenses { get; set; }
    
    public DbSet<SyncHistory> SyncHistories { get; set; } // Bu satırı ekliyoruz

}