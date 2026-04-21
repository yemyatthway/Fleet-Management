namespace FleetManagement.Api.Models;

public sealed class UserCodeOption
{
    public int Id { get; set; }

    public required string Type { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
