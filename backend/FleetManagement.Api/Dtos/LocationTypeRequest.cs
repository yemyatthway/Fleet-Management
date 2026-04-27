namespace FleetManagement.Api.Dtos;

public record LocationTypeRequest(
  string Name,
  string Code,
  string? Description,
  string Status);
