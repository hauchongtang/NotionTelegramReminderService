using Microsoft.EntityFrameworkCore;
using NotionReminderService.Models.Transport;

namespace NotionReminderService.Models;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<BusStop> BusStops { get; set; }
}