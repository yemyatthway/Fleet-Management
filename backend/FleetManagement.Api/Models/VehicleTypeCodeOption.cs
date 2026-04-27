namespace FleetManagement.Api.Models;

public sealed class VehicleTypeCodeOption
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    public string? Description { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
