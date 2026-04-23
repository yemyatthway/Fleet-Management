namespace FleetManagement.Api.Models;

public sealed class LocationCodeOption
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    public required string LocationType { get; set; }

    public required string Address { get; set; }

    public required string City { get; set; }

    public required string Country { get; set; }

    public string? ContactPerson { get; set; }

    public required string Phone { get; set; }

    public required string OperatingHours { get; set; }

    public string? Description { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
