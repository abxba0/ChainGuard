using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChainGuard.Data;

/// <summary>
/// Factory for creating DbContext instances at design time (for migrations).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChainGuardDbContext>
{
    public ChainGuardDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChainGuardDbContext>();

        // Use SQLite for design-time migrations
        optionsBuilder.UseSqlite("Data Source=chainguard_design.db");

        return new ChainGuardDbContext(optionsBuilder.Options);
    }
}
