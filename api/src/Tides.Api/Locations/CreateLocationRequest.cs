using System.ComponentModel.DataAnnotations;

namespace Tides.Api.Locations;

public sealed record CreateLocationRequest(
    [Required, StringLength(Location.NameMaxLength)] string Name,
    [Required, Range(-90, 90)] double? Latitude,
    [Required, Range(-180, 180)] double? Longitude
);
