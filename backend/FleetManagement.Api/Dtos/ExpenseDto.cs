namespace FleetManagement.Api.Dtos;

public record ExpenseDto(
  int Id,
  string ExpenseDate,
  string ExpenseType,
  string? VehicleId,
  string? TripNumber,
  string? DriverName,
  decimal Amount,
  string Status,
  string? Notes,
  DateTime CreatedAt,
  DateTime UpdatedAt);

public record ExpenseRequest(
  string ExpenseDate,
  string ExpenseType,
  string? VehicleId,
  string? TripNumber,
  string? DriverName,
  decimal Amount,
  string Status,
  string? Notes);
