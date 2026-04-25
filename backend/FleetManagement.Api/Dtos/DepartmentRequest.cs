namespace FleetManagement.Api.Dtos;

public record DepartmentRequest(
  string Name,
  string? Description,
  string Status);
