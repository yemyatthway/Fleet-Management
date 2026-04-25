namespace FleetManagement.Api.Dtos;

public record MaintenanceTicketPagedResult(
  IReadOnlyList<MaintenanceTicketDto> Items,
  int Total,
  MaintenanceTicketStatsDto Stats);
