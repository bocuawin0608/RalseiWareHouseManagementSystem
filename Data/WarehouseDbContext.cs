using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RalseiWarehouse_v2.Models;

namespace RalseiWarehouse_v2.Data;

// Chapter 08 pattern: DbContext + DbSet per table, connection string
// from appsettings.json inside OnConfiguring.
public class WarehouseDbContext : DbContext
{
    public DbSet<Role> Roles { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<OrderHeader> OrderHeaders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }
    public DbSet<WorkTask> WorkTasks { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseSqlServer(config.GetConnectionString("RalseiWarehouse"));
        }
    }
}
