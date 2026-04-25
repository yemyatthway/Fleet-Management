namespace FleetManagement.Api.Dtos;

public record DepartmentDto(
  int Id,
  string Name,
  string? Description,
  string Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);
