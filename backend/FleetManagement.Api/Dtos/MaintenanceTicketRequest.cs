namespace FleetManagement.Api.Dtos;

public record MaintenanceTicketRequest(
  string Vehicle,
  string VehicleId,
  string Issue,
  string Details,
  string ReportedDate,
  string Mechanic,
  string Status);
