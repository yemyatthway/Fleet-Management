namespace FleetManagement.Api.Dtos;

public record FuelTypeRequest(
  string Name,
  string Code,
  string? Description,
  string Status);
