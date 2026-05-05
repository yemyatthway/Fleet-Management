namespace FleetManagement.Api.Dtos;

public record TripSetupDto(
  int Id,
  string Name,
  string Code,
  string? Description,
  string Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);

public record TripSetupRequest(
  string Name,
  string Code,
  string? Description,
  string Status);
