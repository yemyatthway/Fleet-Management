namespace FleetManagement.Api.Dtos;

public record MaintenanceTicketDto(
  string Id,
  string Vehicle,
  string VehicleId,
  string Issue,
  string Details,
  string ReportedDate,
  string Mechanic,
  string Status,
  DateTime CreatedAt,
  DateTime UpdatedAt);
