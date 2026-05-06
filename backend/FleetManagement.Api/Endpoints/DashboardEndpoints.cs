using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class DashboardEndpoints
{
  public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/dashboard/summary", async (FleetDbContext db) =>
    {
      var vehicles = db.Vehicles.Where(vehicle => vehicle.IsDeleted == 0);
      var trips = db.Trips.Where(trip => trip.IsDeleted == 0);
      var tickets = db.MaintenanceTickets.Where(ticket => ticket.IsDeleted == 0);
      var incidents = db.Incidents.Where(incident => incident.IsDeleted == 0);

      var vehicleStatuses = await SafeDashboardValueAsync(async () => await vehicles
        .GroupBy(vehicle => vehicle.Status)
        .Select(group => new NamedCountDto(group.Key, group.Count()))
        .OrderByDescending(item => item.Value)
        .ToListAsync(), new List<NamedCountDto>());

      var tripStatuses = await SafeDashboardValueAsync(async () => await trips
        .GroupBy(trip => trip.Status)
        .Select(group => new NamedCountDto(group.Key, group.Count()))
        .OrderByDescending(item => item.Value)
        .ToListAsync(), new List<NamedCountDto>());

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

      var metrics = new List<DashboardMetricDto>
      {
        new("Total Vehicles", await SafeDashboardValueAsync(() => vehicles.CountAsync(), 0), "mdi-truck", "info"),
        new("Active Trips", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "In Transit" || trip.Status == "Active" || trip.Status == "Ongoing"), 0), "mdi-map-marker", "success"),
        new("Open Maintenance", await SafeDashboardValueAsync(() => tickets.CountAsync(ticket => ticket.Status != "Completed" && ticket.Status != "Closed"), 0), "mdi-wrench", "warning"),
        new("Incidents", await SafeDashboardValueAsync(() => incidents.CountAsync(), 0), "mdi-alert-circle-outline", "danger"),
        new("Upcoming Expiries", upcomingExpiries.Count, "mdi-calendar-alert", "purple")
      };

      return Results.Ok(new DashboardSummaryDto(metrics, vehicleStatuses, tripStatuses, recentTrips, upcomingExpiries));
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
