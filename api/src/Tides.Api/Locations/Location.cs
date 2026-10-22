using System.ComponentModel.DataAnnotations;

namespace Tides.Api.Locations;

public sealed class Location
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
