namespace FleetManagement.Api.Dtos;

public record MaintenanceTicketStatsDto(
  int Total,
  int Pending,
  int Repairing,
  int Completed);
