namespace FleetManagement.Api.Dtos;

public record IncidentRequest(
  string VehicleId,
  string Driver,
  string Date,
  string Type,
  string Severity,
  string Status,
  string? Cost,
  string? Notes);
