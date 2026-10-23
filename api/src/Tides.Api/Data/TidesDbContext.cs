using Microsoft.EntityFrameworkCore;
using Tides.Api.Imports;
using Tides.Api.Locations;

namespace Tides.Api.Data;

public sealed class TidesDbContext(DbContextOptions<TidesDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Import> Imports => Set<Import>();
    public DbSet<TideEntry> TideEntries => Set<TideEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Import>()
            .HasOne<Location>()
            .WithMany()
            .HasForeignKey(i => i.LocationId);

        modelBuilder.Entity<TideEntry>()
            .HasIndex(e => new { e.LocationId, e.Time, e.Type })
            .IsUnique();
    }
}
