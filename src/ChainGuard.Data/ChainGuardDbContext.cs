using ChainGuard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChainGuard.Data;

/// <summary>
/// Database context for ChainGuard audit chains.
/// </summary>
public class ChainGuardDbContext : DbContext
{
    /// <summary>
    /// Creates a new instance of the ChainGuardDbContext.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public ChainGuardDbContext(DbContextOptions<ChainGuardDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Chains in the database.
    /// </summary>
    public DbSet<ChainEntity> Chains { get; set; }

    /// <summary>
    /// Blocks in the database.
    /// </summary>
    public DbSet<BlockEntity> Blocks { get; set; }

    /// <summary>
    /// Off-chain data in the database.
    /// </summary>
    public DbSet<OffChainDataEntity> OffChainData { get; set; }

    /// <summary>
    /// Configures the model for the database.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure value converters for DateTime to ensure UTC and proper format
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
            }
        }

        // Configure ChainEntity
        modelBuilder.Entity<ChainEntity>(entity =>
        {
            entity.HasKey(e => e.ChainId);
            entity.Property(e => e.ChainName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            // Configure relationship with blocks
            entity.HasMany(e => e.Blocks)
                  .WithOne(b => b.Chain)
                  .HasForeignKey(b => b.ChainId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BlockEntity
        modelBuilder.Entity<BlockEntity>(entity =>
        {
            entity.HasKey(e => e.BlockId);
            entity.Property(e => e.CurrentHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Signature).IsRequired();
            entity.Property(e => e.Nonce).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PayloadHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.PreviousHash).HasMaxLength(64);

            // Create composite unique index on ChainId and BlockHeight
            entity.HasIndex(e => new { e.ChainId, e.BlockHeight })
                  .IsUnique()
                  .HasDatabaseName("IX_Blocks_ChainId_BlockHeight");

            // Create individual indexes for performance
            entity.HasIndex(e => e.ChainId)
                  .HasDatabaseName("IX_Blocks_ChainId");
            
            entity.HasIndex(e => e.BlockHeight)
                  .HasDatabaseName("IX_Blocks_BlockHeight");
            
            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_Blocks_Timestamp");

            // Configure relationship with off-chain data
            entity.HasMany(e => e.OffChainData)
                  .WithOne(o => o.Block)
                  .HasForeignKey(o => o.BlockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OffChainDataEntity
        modelBuilder.Entity<OffChainDataEntity>(entity =>
        {
            entity.HasKey(e => e.DataId);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(100);

            // Create indexes for performance
            entity.HasIndex(e => e.BlockId)
                  .HasDatabaseName("IX_OffChainData_BlockId");
            
            entity.HasIndex(e => e.DataType)
                  .HasDatabaseName("IX_OffChainData_DataType");
            
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_OffChainData_CreatedAt");
        });
    }
}
