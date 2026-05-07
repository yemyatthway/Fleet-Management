using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class ReportsEndpoints
{
  public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/reports/{reportType}", async (
      string reportType,
      HttpRequest httpRequest,
      FleetDbContext db,
      string? dateFrom = null,
      string? dateTo = null,
      string? status = null,
      string? vehicleId = null,
      string? driver = null) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "reports", PermissionAction.View);
      if (permissionError is not null) return permissionError;
      var roleId = httpRequest.Headers["X-Fleet-Role-Id"].FirstOrDefault();
      var normalizedReportType = reportType.ToLowerInvariant();
      if (string.Equals(roleId, "dispatcher", StringComparison.OrdinalIgnoreCase) &&
          normalizedReportType is not ("vehicles" or "trips"))
      {
        return Results.Json(
          new ApiError("Dispatchers can only access vehicle and trip reports."),
          statusCode: StatusCodes.Status403Forbidden);
      }

      DateTime? parsedDateFrom = DateTime.TryParse(dateFrom, out var startDate) ? startDate.Date : null;
      DateTime? parsedDateTo = DateTime.TryParse(dateTo, out var endDate) ? endDate.Date.AddDays(1).AddTicks(-1) : null;

      object rows = normalizedReportType switch
      {
        "vehicles" => await db.Vehicles.AsNoTracking()
          .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (parsedDateFrom == null || item.CreatedAt >= parsedDateFrom) && (parsedDateTo == null || item.CreatedAt <= parsedDateTo))
          .Select(item => new { item.Id, item.Plate, item.Type, item.Status, item.Driver, item.Depot })
          .ToListAsync(),
        "trips" => await db.Trips.AsNoTracking()
          .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (string.IsNullOrWhiteSpace(vehicleId) || item.VehicleId == vehicleId) && (string.IsNullOrWhiteSpace(driver) || item.DriverName == driver) && (string.IsNullOrWhiteSpace(dateFrom) || string.Compare(item.DepartureDateTime, dateFrom) >= 0) && (string.IsNullOrWhiteSpace(dateTo) || string.Compare(item.DepartureDateTime, dateTo) <= 0))
          .Select(item => new { item.TripNumber, item.VehicleId, item.DriverName, item.Status, item.PickupLocation, item.DropoffLocation })
          .ToListAsync(),
        "maintenance" => await db.MaintenanceTickets.AsNoTracking()
          .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (string.IsNullOrWhiteSpace(vehicleId) || item.VehicleId == vehicleId) && (string.IsNullOrWhiteSpace(dateFrom) || string.Compare(item.ReportedDate, dateFrom) >= 0) && (string.IsNullOrWhiteSpace(dateTo) || string.Compare(item.ReportedDate, dateTo) <= 0))
          .Select(item => new { item.Id, item.VehicleId, item.Issue, item.Mechanic, item.Status, item.ReportedDate })
          .ToListAsync(),
        "drivers" => await db.Users.AsNoTracking().Include(item => item.Role)
          .Where(item => item.IsDeleted == 0 && item.Role != null && item.Role.Name == "Driver" && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (parsedDateFrom == null || item.CreatedAt >= parsedDateFrom) && (parsedDateTo == null || item.CreatedAt <= parsedDateTo))
          .Select(item => new { item.EmployeeId, item.Name, item.Email, item.Phone, item.Status, item.LicenseExpiry })
          .ToListAsync(),
        "expenses" => await db.Expenses.AsNoTracking()
          .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (string.IsNullOrWhiteSpace(vehicleId) || item.VehicleId == vehicleId) && (string.IsNullOrWhiteSpace(driver) || item.DriverName == driver) && (string.IsNullOrWhiteSpace(dateFrom) || string.Compare(item.ExpenseDate, dateFrom) >= 0) && (string.IsNullOrWhiteSpace(dateTo) || string.Compare(item.ExpenseDate, dateTo) <= 0))
          .Select(item => new { item.ExpenseDate, item.ExpenseType, item.VehicleId, item.TripNumber, item.DriverName, item.Amount, item.Status })
          .ToListAsync(),
        "audit-logs" => await db.AuditLogs.AsNoTracking()
          .Where(item => (parsedDateFrom == null || item.CreatedAt >= parsedDateFrom) && (parsedDateTo == null || item.CreatedAt <= parsedDateTo))
          .OrderByDescending(item => item.CreatedAt)
          .Select(item => new { item.CreatedAt, item.RoleId, item.ModuleKey, item.Action, item.EntityId, item.Description })
          .ToListAsync(),
        _ => Array.Empty<object>()
      };

      return Results.Ok(rows);
    });

    return app;
  }
}
