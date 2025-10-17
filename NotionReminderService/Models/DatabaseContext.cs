using Microsoft.EntityFrameworkCore;
using NotionReminderService.Models.Transport;
using NotionReminderService.Models.Weather;
using NotionReminderService.Models.Weather.Rainfall;

namespace NotionReminderService.Models;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<BusStop> BusStops { get; set; }
    public DbSet<RainfallStationCached> RainfallStations { get; set; }
    public DbSet<Rainfall?> Rainfalls { get; set; }
    public DbSet<RainfallSlot> RainfallSlots { get; set; }
    public DbSet<RainfallIntensity> RainfallIntensities { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Rainfall>()
            .Property(r => r.RainfallId)
            .ValueGeneratedOnAdd();

        builder.Entity<RainfallSlot>()
            .Property(r => r.RainfallSlotId)
            .ValueGeneratedOnAdd();
    }
}