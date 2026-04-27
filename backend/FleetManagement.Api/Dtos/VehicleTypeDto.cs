namespace FleetManagement.Api.Dtos;

public record VehicleTypeDto(
  int Id,
  string Name,
  string Code,
  string? Description,
  string Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);
