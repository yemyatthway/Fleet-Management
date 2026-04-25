namespace FleetManagement.Api.Dtos;

public record RoleDto(
  string Id,
  string Code,
  string Name,
  string Description,
  string Status,
  int Members,
  DateTime CreatedAt,
  DateTime UpdatedAt);
