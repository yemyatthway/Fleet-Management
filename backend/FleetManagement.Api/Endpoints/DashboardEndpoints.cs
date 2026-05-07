using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class DashboardEndpoints
{
  public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/dashboard/summary", async (HttpRequest request, FleetDbContext db) =>
    {
      var roleId = request.Headers["X-Fleet-Role-Id"].FirstOrDefault() ?? string.Empty;
      var userName = request.Headers["X-Fleet-User-Name"].FirstOrDefault() ?? string.Empty;
      var normalizedRoleId = roleId.Trim().ToLowerInvariant();
      var normalizedUserName = userName.Trim().ToLowerInvariant();

      var vehicles = db.Vehicles.Where(vehicle => vehicle.IsDeleted == 0);
      var trips = db.Trips.Where(trip => trip.IsDeleted == 0);
      var tickets = db.MaintenanceTickets.Where(ticket => ticket.IsDeleted == 0);
      var incidents = db.Incidents.Where(incident => incident.IsDeleted == 0);
      var inventoryParts = db.InventoryParts.Where(part => part.IsDeleted == 0);

      if (normalizedRoleId == "driver" && !string.IsNullOrWhiteSpace(normalizedUserName))
      {
        trips = trips.Where(trip =>
          trip.DriverName.ToLower() == normalizedUserName ||
          (trip.CoDriverName != null && trip.CoDriverName.ToLower() == normalizedUserName));
        vehicles = vehicles.Where(vehicle => vehicle.Driver.ToLower() == normalizedUserName);
        incidents = incidents.Where(incident => incident.Driver.ToLower() == normalizedUserName);
      }
      else if (normalizedRoleId == "dispatcher" && !string.IsNullOrWhiteSpace(normalizedUserName))
      {
        trips = trips.Where(trip => trip.DispatcherName.ToLower() == normalizedUserName);
      }
      else if (normalizedRoleId == "mechanic" && !string.IsNullOrWhiteSpace(normalizedUserName))
      {
        tickets = tickets.Where(ticket => ticket.Mechanic.ToLower() == normalizedUserName);
      }

      var vehicleStatuses = await SafeDashboardValueAsync(
        () => BuildVehicleStatusCountsAsync(vehicles),
        new List<NamedCountDto>());

      var primaryStatuses = normalizedRoleId == "mechanic"
        ? await SafeDashboardValueAsync(
          () => BuildMaintenanceStatusCountsAsync(tickets),
          new List<NamedCountDto>())
        : await SafeDashboardValueAsync(
          () => BuildTripStatusCountsAsync(trips),
          new List<NamedCountDto>());

      var recentTripRows = await SafeDashboardValueAsync(async () => await trips
        .OrderByDescending(trip => trip.UpdatedAt)
        .ThenByDescending(trip => trip.Id)
        .Take(8)
        .Select(trip => new
        {
          trip.Id,
          trip.TripNumber,
          trip.VehiclePlate,
          trip.DriverName,
          trip.PickupLocation,
          trip.DropoffLocation,
          trip.Status,
          trip.TripType,
          trip.Priority
        })
        .ToListAsync(), []);

      var recentTrips = recentTripRows
        .Select(trip => new DashboardRecentTripDto(
          trip.Id,
          trip.TripNumber,
          trip.VehiclePlate,
          trip.DriverName,
          $"{trip.PickupLocation} to {trip.DropoffLocation}",
          trip.Status,
          string.IsNullOrWhiteSpace(trip.TripType) && string.IsNullOrWhiteSpace(trip.Priority)
            ? "-"
            : $"{trip.TripType} | {trip.Priority}"))
        .ToList();

      var upcomingExpiries = (await SafeDashboardValueAsync(
          async () => await vehicles.AsNoTracking().ToListAsync(),
          new List<Vehicle>()))
        .SelectMany(GetUpcomingVehicleExpiries)
        .OrderBy(expiry => expiry.DaysRemaining)
        .Take(8)
        .ToList();

      var metrics = await BuildMetricsAsync(
        normalizedRoleId,
        vehicles,
        trips,
        tickets,
        incidents,
        inventoryParts,
        upcomingExpiries.Count);

      return Results.Ok(new DashboardSummaryDto(metrics, vehicleStatuses, primaryStatuses, recentTrips, upcomingExpiries));
    });

    return app;
  }

  private static async Task<T> SafeDashboardValueAsync<T>(Func<Task<T>> load, T fallback)
  {
    try
    {
      return await load();
    }
    catch
    {
      return fallback;
    }
  }

  private static async Task<List<NamedCountDto>> BuildVehicleStatusCountsAsync(IQueryable<Vehicle> vehicles)
  {
    var statuses = await vehicles.Select(vehicle => vehicle.Status).ToListAsync();
    return CountStatuses(statuses);
  }

  private static async Task<List<NamedCountDto>> BuildTripStatusCountsAsync(IQueryable<Trip> trips)
  {
    var statuses = await trips.Select(trip => trip.Status).ToListAsync();
    return CountStatuses(statuses);
  }

  private static async Task<List<NamedCountDto>> BuildMaintenanceStatusCountsAsync(IQueryable<MaintenanceTicket> tickets)
  {
    var statuses = await tickets.Select(ticket => ticket.Status).ToListAsync();
    return CountStatuses(statuses);
  }

  private static List<NamedCountDto> CountStatuses(IEnumerable<string?> statuses) =>
    statuses
      .Select(status => string.IsNullOrWhiteSpace(status) ? "Unknown" : status.Trim())
      .GroupBy(status => status, StringComparer.OrdinalIgnoreCase)
      .Select(group => new NamedCountDto(group.First(), group.Count()))
      .OrderByDescending(item => item.Value)
      .ThenBy(item => item.Name)
      .ToList();

  private static async Task<IReadOnlyList<DashboardMetricDto>> BuildMetricsAsync(
    string roleId,
    IQueryable<Vehicle> vehicles,
    IQueryable<Trip> trips,
    IQueryable<MaintenanceTicket> tickets,
    IQueryable<Incident> incidents,
    IQueryable<InventoryPart> inventoryParts,
    int upcomingExpiryCount)
  {
    if (roleId == "driver")
    {
      return new List<DashboardMetricDto>
      {
        new("My Trips", await SafeDashboardValueAsync(() => trips.CountAsync(), 0), "mdi-map-marker-path", "info"),
        new("Active Trips", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "In Transit" || trip.Status == "Active" || trip.Status == "Ongoing"), 0), "mdi-truck-fast", "success"),
        new("Assigned Vehicles", await SafeDashboardValueAsync(() => vehicles.CountAsync(), 0), "mdi-truck", "warning"),
        new("My Incidents", await SafeDashboardValueAsync(() => incidents.CountAsync(), 0), "mdi-alert-circle-outline", "danger")
      };
    }

    if (roleId == "dispatcher")
    {
      return new List<DashboardMetricDto>
      {
        new("Assigned Trips", await SafeDashboardValueAsync(() => trips.CountAsync(), 0), "mdi-map-marker-path", "info"),
        new("Active Dispatches", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "In Transit" || trip.Status == "Active" || trip.Status == "Ongoing"), 0), "mdi-truck-fast", "success"),
        new("Pending Trips", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "Pending" || trip.Status == "Scheduled"), 0), "mdi-clock-outline", "warning"),
        new("Completed Trips", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "Completed"), 0), "mdi-check-circle-outline", "purple")
      };
    }

    if (roleId == "mechanic")
    {
      return new List<DashboardMetricDto>
      {
        new("Assigned Tickets", await SafeDashboardValueAsync(() => tickets.CountAsync(), 0), "mdi-wrench", "info"),
        new("Open Tickets", await SafeDashboardValueAsync(() => tickets.CountAsync(ticket => ticket.Status != "Completed" && ticket.Status != "Closed"), 0), "mdi-alert-circle-outline", "warning"),
        new("Low Stock Parts", await SafeDashboardValueAsync(() => inventoryParts.CountAsync(part => part.Stock <= part.ReorderPoint), 0), "mdi-package-variant-closed", "danger"),
        new("Fleet Incidents", await SafeDashboardValueAsync(() => incidents.CountAsync(), 0), "mdi-clipboard-alert-outline", "purple")
      };
    }

    return new List<DashboardMetricDto>
    {
      new("Total Vehicles", await SafeDashboardValueAsync(() => vehicles.CountAsync(), 0), "mdi-truck", "info"),
      new("Active Trips", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "In Transit" || trip.Status == "Active" || trip.Status == "Ongoing"), 0), "mdi-map-marker", "success"),
      new("Open Maintenance", await SafeDashboardValueAsync(() => tickets.CountAsync(ticket => ticket.Status != "Completed" && ticket.Status != "Closed"), 0), "mdi-wrench", "warning"),
      new("Incidents", await SafeDashboardValueAsync(() => incidents.CountAsync(), 0), "mdi-alert-circle-outline", "danger"),
      new("Upcoming Expiries", upcomingExpiryCount, "mdi-calendar-alert", "purple")
    };
  }

  private static IEnumerable<DashboardUpcomingExpiryDto> GetUpcomingVehicleExpiries(Vehicle vehicle)
  {
    foreach (var expiry in GetVehicleExpiryCandidates(vehicle))
    {
      if (!DateTime.TryParse(expiry.Date, out var parsedDate)) continue;
      var daysRemaining = (parsedDate.Date - DateTime.UtcNow.Date).Days;
      if (daysRemaining < 0 || daysRemaining > 60) continue;
      yield return new DashboardUpcomingExpiryDto("Vehicle", $"{vehicle.Id} {expiry.Label}", parsedDate.ToString("yyyy-MM-dd"), daysRemaining);
    }
  }

  private static IEnumerable<(string Label, string? Date)> GetVehicleExpiryCandidates(Vehicle vehicle)
  {
    yield return ("registration", vehicle.RegistrationExpiry);
    yield return ("road tax", vehicle.RoadTaxExpiry);
    yield return ("insurance", vehicle.InsuranceExpiry);
    yield return ("inspection", vehicle.InspectionDue);
  }
}
