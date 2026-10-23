using System.ComponentModel.DataAnnotations;

namespace Tides.Api.Locations;

public sealed class Location
{
    public const int NameMaxLength = 100;

    public int Id { get; set; }

    [MaxLength(NameMaxLength)]
    public required string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
