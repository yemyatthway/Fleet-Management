namespace FleetManagement.Api.Dtos;

public record LocationDto(
  int Id,
  string Name,
  string Code,
  string Type,
  string Address,
  string City,
  string Country,
  string? ContactPerson,
  string Phone,
  string OperatingHours,
  string? Notes,
  string Status,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);
