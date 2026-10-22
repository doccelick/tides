using Microsoft.EntityFrameworkCore;
using Tides.Api.Locations;

namespace Tides.Api.Data;

public sealed class TidesDbContext(DbContextOptions<TidesDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();
}
