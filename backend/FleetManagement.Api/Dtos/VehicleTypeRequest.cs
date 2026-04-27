namespace FleetManagement.Api.Dtos;

public record VehicleTypeRequest(
  string Name,
  string Code,
  string? Description,
  string Status);
