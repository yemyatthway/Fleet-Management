namespace FleetManagement.Api.Dtos;

public sealed record UserCodeOptionRequest(
    string Type,
    string Name,
    string? Description,
    string Status);
