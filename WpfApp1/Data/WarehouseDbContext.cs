using Microsoft.EntityFrameworkCore;
using RalseiWarehouse.Models;
using Object = RalseiWarehouse.Models.Object;

namespace RalseiWarehouse.Data;

/// <summary>
/// Entity Framework Core database context for the warehouse management system.
/// Provides access to all tables: Unit, Supplier, Customer, Object, Input, InputInfo, Output, OutputInfo, Role, User.
/// </summary>
public class WarehouseDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarehouseDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WarehouseDbContext"/> class with a SQL Server connection string.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public WarehouseDbContext(string connectionString)
        : base(new DbContextOptionsBuilder<WarehouseDbContext>()
              .UseSqlServer(connectionString).Options) { }

    /// <summary>Gets the units table.</summary>
    public DbSet<Unit> Units => Set<Unit>();

    /// <summary>Gets the suppliers table.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>Gets the customers table.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the products (objects) table.</summary>
    public DbSet<Object> Objects => Set<Object>();

    /// <summary>Gets the import receipts table.</summary>
    public DbSet<Input> Inputs => Set<Input>();

    /// <summary>Gets the import line items table.</summary>
    public DbSet<InputInfo> InputInfos => Set<InputInfo>();

    /// <summary>Gets the export receipts table.</summary>
    public DbSet<Output> Outputs => Set<Output>();

    /// <summary>Gets the export line items table.</summary>
    public DbSet<OutputInfo> OutputInfos => Set<OutputInfo>();

    /// <summary>Gets the roles table.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Gets the users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId);
            entity.ToTable("Unit");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId);
            entity.ToTable("Supplier");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.ContractDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.ToTable("Customer");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.ContractDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Object>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Object");
            entity.Property(e => e.Id).HasMaxLength(128);

            entity.HasOne(d => d.Unit).WithMany(p => p.Objects)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Supplier).WithMany(p => p.Objects)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Input>(entity =>
        {
            entity.HasKey(e => e.InputId);
            entity.ToTable("Input");
            entity.Property(e => e.InputId).HasMaxLength(128);
            entity.Property(e => e.DateInput).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<InputInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("InputInfo");
            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.ObjectId).HasMaxLength(128);
            entity.Property(e => e.InputId).HasMaxLength(128);
            entity.Property(e => e.InputPrice).HasDefaultValue(0.0);
            entity.Property(e => e.OutputPrice).HasDefaultValue(0.0);

            entity.HasOne(d => d.Object).WithMany(p => p.InputInfos)
                .HasForeignKey(d => d.ObjectId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Input).WithMany(p => p.InputInfos)
                .HasForeignKey(d => d.InputId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Output>(entity =>
        {
            entity.HasKey(e => e.OutputId);
            entity.ToTable("Output");
            entity.Property(e => e.OutputId).HasMaxLength(128);
            entity.Property(e => e.DateOutput).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<OutputInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("OutputInfo");
            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.ObjectId).HasMaxLength(128);
            entity.Property(e => e.OutputId).HasMaxLength(128);

            entity.HasOne(d => d.Object).WithMany(p => p.OutputInfos)
                .HasForeignKey(d => d.ObjectId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Output).WithMany(p => p.OutputInfos)
                .HasForeignKey(d => d.OutputId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Customer).WithMany(p => p.OutputInfos)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.ToTable("Role");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.ToTable("User");
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.Property(e => e.UserName).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
