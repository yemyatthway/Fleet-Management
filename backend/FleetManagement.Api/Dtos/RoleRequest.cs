namespace FleetManagement.Api.Dtos;

public sealed record RoleRequest(
    string Name,
    string Description,
    string Status);
