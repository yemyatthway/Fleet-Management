namespace FleetManagement.Api.Dtos;

public record RoleRequest(
  string Name,
  string Description,
  string Status);
