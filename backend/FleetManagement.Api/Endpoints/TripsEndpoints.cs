using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class TripsEndpoints
{
  public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/trips", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? status = null,
      string? tripType = null,
      string? sortBy = "id",
      string? sortOrder = "desc") =>
    {
      var query = db.Trips.Where(trip => trip.IsDeleted == 0).AsNoTracking().AsQueryable();
      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(trip =>
          trip.TripNumber.ToLower().Contains(normalizedSearch) ||
          trip.PickupLocation.ToLower().Contains(normalizedSearch) ||
          trip.DropoffLocation.ToLower().Contains(normalizedSearch) ||
          trip.DriverName.ToLower().Contains(normalizedSearch) ||
          (trip.CoDriverName ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          trip.DispatcherName.ToLower().Contains(normalizedSearch) ||
          trip.CustomerName.ToLower().Contains(normalizedSearch) ||
          trip.VehicleId.ToLower().Contains(normalizedSearch) ||
          trip.VehiclePlate.ToLower().Contains(normalizedSearch) ||
          trip.CargoType.ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(status) && status != "All")
      {
        var normalizedStatus = status.Trim().ToLower();
        query = query.Where(trip => trip.Status.ToLower() == normalizedStatus);
      }

      if (!string.IsNullOrWhiteSpace(tripType) && tripType != "All")
      {
        var normalizedType = tripType.Trim().ToLower();
        query = query.Where(trip => trip.TripType.ToLower() == normalizedType);
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("tripnumber", "asc") => query.OrderBy(trip => trip.TripNumber),
        ("tripnumber", _) => query.OrderByDescending(trip => trip.TripNumber),
        ("status", "asc") => query.OrderBy(trip => trip.Status),
        ("status", _) => query.OrderByDescending(trip => trip.Status),
        ("triptype", "asc") => query.OrderBy(trip => trip.TripType),
        ("triptype", _) => query.OrderByDescending(trip => trip.TripType),
        ("departure", "asc") => query.OrderBy(trip => trip.DepartureDateTime),
        ("departure", _) => query.OrderByDescending(trip => trip.DepartureDateTime),
        ("id", "asc") => query.OrderBy(trip => trip.Id),
        _ => query.OrderByDescending(trip => trip.Id)
      };

      var total = await query.CountAsync();
      var records = await query.Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
      return Results.Ok(new PagedResult<TripDto>(records.Select(ToTripDto).ToList(), total));
    });

    app.MapPost("/api/trips", async (TripRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateTripRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
      var duplicate = await db.Trips.AnyAsync(trip => trip.IsDeleted == 0 && trip.TripNumber.ToLower() == request.TripNumber!.Trim().ToLower());
      if (duplicate) return Results.BadRequest(new ApiError("Trip number already exists."));

      var now = DateTime.UtcNow;
      var trip = ApplyTripRequest(new Trip { CreatedAt = now, IsDeleted = 0 }, request);
      trip.UpdatedAt = now;
      db.Trips.Add(trip);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "trips", "Create", request.TripNumber!.Trim(), $"Created trip {request.TripNumber!.Trim()}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToTripDto(trip));
    });

    app.MapPut("/api/trips/{id:int}", async (int id, TripRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateTripRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
      var trip = await db.Trips.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (trip is null) return Results.NotFound(new ApiError("Trip not found."));
      var duplicate = await db.Trips.AnyAsync(item => item.Id != id && item.IsDeleted == 0 && item.TripNumber.ToLower() == request.TripNumber!.Trim().ToLower());
      if (duplicate) return Results.BadRequest(new ApiError("Trip number already exists."));

      var oldStatus = trip.Status;
      ApplyTripRequest(trip, request);
      trip.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "Trip", trip.Id.ToString(), oldStatus, trip.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "trips", "Edit", trip.Id.ToString(), $"Updated trip {trip.TripNumber}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToTripDto(trip));
    });

    app.MapDelete("/api/trips/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var trip = await db.Trips.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (trip is null) return Results.NotFound(new ApiError("Trip not found."));
      trip.IsDeleted = 1;
      trip.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "trips", "Delete", trip.Id.ToString(), $"Deleted trip {trip.TripNumber}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static TripDto ToTripDto(Trip trip) =>
    new(
      trip.Id,
      trip.TripNumber,
      trip.TripType,
      trip.Status,
      trip.Priority,
      trip.CustomerName,
      trip.Department,
      trip.CostCenter,
      trip.VehicleId,
      trip.VehiclePlate,
      trip.TrailerNumber,
      trip.DriverName,
      trip.CoDriverName,
      trip.DispatcherName,
      trip.CargoType,
      trip.LoadWeightKg,
      trip.LoadVolumeM3,
      trip.PickupLocation,
      trip.DropoffLocation,
      trip.PickupContact,
      trip.DropoffContact,
      trip.DepartureDateTime,
      trip.EstimatedArrival,
      trip.ActualArrival,
      trip.PlannedDistanceKm,
      trip.StartingOdometerKm,
      trip.CurrentOdometerKm,
      trip.EndingOdometerKm,
      trip.FuelIssuedLiters,
      trip.TollEstimate,
      trip.PermitRequired,
      trip.TemperatureControlled,
      trip.TemperatureRange,
      trip.SpecialInstructions,
      trip.DriverNotes,
      trip.CreatedAt,
      trip.UpdatedAt);

  private static string? ValidateTripRequest(TripRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.TripNumber)) return "Trip number is required.";
    if (string.IsNullOrWhiteSpace(request.TripType)) return "Trip type is required.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Trip status is required.";
    if (string.IsNullOrWhiteSpace(request.Priority)) return "Priority is required.";
    if (string.IsNullOrWhiteSpace(request.CustomerName)) return "Customer is required.";
    if (string.IsNullOrWhiteSpace(request.Department)) return "Department is required.";
    if (string.IsNullOrWhiteSpace(request.VehicleId)) return "Vehicle is required.";
    if (string.IsNullOrWhiteSpace(request.VehiclePlate)) return "Vehicle plate is required.";
    if (string.IsNullOrWhiteSpace(request.DriverName)) return "Driver is required.";
    if (string.IsNullOrWhiteSpace(request.DispatcherName)) return "Dispatcher is required.";
    if (string.IsNullOrWhiteSpace(request.CargoType)) return "Cargo type is required.";
    if (string.IsNullOrWhiteSpace(request.PickupLocation)) return "Pickup location is required.";
    if (string.IsNullOrWhiteSpace(request.DropoffLocation)) return "Dropoff location is required.";
    if (string.IsNullOrWhiteSpace(request.DepartureDateTime)) return "Departure date and time is required.";
    if (string.IsNullOrWhiteSpace(request.EstimatedArrival)) return "Estimated arrival is required.";
    return null;
  }

  private static Trip ApplyTripRequest(Trip trip, TripRequest request)
  {
    trip.TripNumber = request.TripNumber!.Trim();
    trip.TripType = request.TripType!.Trim();
    trip.Status = request.Status!.Trim();
    trip.Priority = request.Priority!.Trim();
    trip.CustomerName = request.CustomerName!.Trim();
    trip.Department = request.Department!.Trim();
    trip.CostCenter = NormalizeOptional(request.CostCenter);
    trip.VehicleId = request.VehicleId!.Trim();
    trip.VehiclePlate = request.VehiclePlate!.Trim();
    trip.TrailerNumber = NormalizeOptional(request.TrailerNumber);
    trip.DriverName = request.DriverName!.Trim();
    trip.CoDriverName = NormalizeOptional(request.CoDriverName);
    trip.DispatcherName = request.DispatcherName!.Trim();
    trip.CargoType = request.CargoType!.Trim();
    trip.LoadWeightKg = request.LoadWeightKg;
    trip.LoadVolumeM3 = request.LoadVolumeM3;
    trip.PickupLocation = request.PickupLocation!.Trim();
    trip.DropoffLocation = request.DropoffLocation!.Trim();
    trip.PickupContact = NormalizeOptional(request.PickupContact);
    trip.DropoffContact = NormalizeOptional(request.DropoffContact);
    trip.DepartureDateTime = request.DepartureDateTime!.Trim();
    trip.EstimatedArrival = request.EstimatedArrival!.Trim();
    trip.ActualArrival = NormalizeOptional(request.ActualArrival);
    trip.PlannedDistanceKm = request.PlannedDistanceKm;
    trip.StartingOdometerKm = request.StartingOdometerKm;
    trip.CurrentOdometerKm = request.CurrentOdometerKm;
    trip.EndingOdometerKm = request.EndingOdometerKm;
    trip.FuelIssuedLiters = request.FuelIssuedLiters;
    trip.TollEstimate = request.TollEstimate;
    trip.PermitRequired = request.PermitRequired;
    trip.TemperatureControlled = request.TemperatureControlled;
    trip.TemperatureRange = NormalizeOptional(request.TemperatureRange);
    trip.SpecialInstructions = NormalizeOptional(request.SpecialInstructions);
    trip.DriverNotes = NormalizeOptional(request.DriverNotes);
    return trip;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
