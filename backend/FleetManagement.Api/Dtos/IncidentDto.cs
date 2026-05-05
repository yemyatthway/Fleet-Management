namespace FleetManagement.Api.Dtos;

public record IncidentDto(
  string Id,
  string VehicleId,
  string Driver,
  string Date,
  string Type,
  string Severity,
  string Status,
  string? Cost,
  string? Notes,
  DateTime CreatedAt,
  DateTime UpdatedAt);
