using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Email;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FleetManagement.Api.Endpoints;

public static class TripsEndpoints
{
  public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/trips", async (
      HttpRequest request,
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? status = null,
      string? tripType = null,
      string? scope = null,
      string? sortBy = "id",
      string? sortOrder = "desc") =>
    {
      var query = db.Trips.Where(trip => trip.IsDeleted == 0).AsNoTracking().AsQueryable();
      var roleId = AuditLogWriter.GetRequestRoleId(request);
      var userName = request.Headers["X-Fleet-User-Name"].FirstOrDefault();
      var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "mine" : scope.Trim().ToLowerInvariant();
      if (normalizedScope != "all" && !string.IsNullOrWhiteSpace(userName))
      {
        var normalizedUserName = userName.Trim().ToLower();
        if (string.Equals(roleId, "driver", StringComparison.OrdinalIgnoreCase))
        {
          query = query.Where(trip =>
            trip.DriverName.ToLower() == normalizedUserName ||
            (trip.CoDriverName ?? string.Empty).ToLower() == normalizedUserName);
        }
        else if (string.Equals(roleId, "dispatcher", StringComparison.OrdinalIgnoreCase))
        {
          query = query.Where(trip => trip.DispatcherName.ToLower() == normalizedUserName);
        }
      }

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

    app.MapPost("/api/trips", async (TripRequest request, HttpRequest httpRequest, FleetDbContext db, IEmailSender emailSender) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateTripRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
      var assignmentError = await ValidateVehicleDriverAssignmentAsync(db, request);
      if (assignmentError is not null) return Results.BadRequest(new ApiError(assignmentError));
      var duplicate = await db.Trips.AnyAsync(trip => trip.IsDeleted == 0 && trip.TripNumber.ToLower() == request.TripNumber!.Trim().ToLower());
      if (duplicate) return Results.BadRequest(new ApiError("Trip number already exists."));

      var now = DateTime.UtcNow;
      var trip = ApplyTripRequest(new Trip { CreatedAt = now, IsDeleted = 0 }, request);
      trip.UpdatedAt = now;
      db.Trips.Add(trip);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "trips", "Create", request.TripNumber!.Trim(), $"Created trip {request.TripNumber!.Trim()}.");
      await db.SaveChangesAsync();
      await SendTripAssignmentEmailAsync(db, emailSender, trip);
      return Results.Ok(ToTripDto(trip));
    });

    app.MapPut("/api/trips/{id:int}", async (int id, TripRequest request, HttpRequest httpRequest, FleetDbContext db, IEmailSender emailSender) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateTripRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
      var assignmentError = await ValidateVehicleDriverAssignmentAsync(db, request);
      if (assignmentError is not null) return Results.BadRequest(new ApiError(assignmentError));
      var trip = await db.Trips.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (trip is null) return Results.NotFound(new ApiError("Trip not found."));
      var duplicate = await db.Trips.AnyAsync(item => item.Id != id && item.IsDeleted == 0 && item.TripNumber.ToLower() == request.TripNumber!.Trim().ToLower());
      if (duplicate) return Results.BadRequest(new ApiError("Trip number already exists."));

      var oldStatus = trip.Status;
      var oldDriver = trip.DriverName;
      var changes = BuildTripChanges(trip, request);
      ApplyTripRequest(trip, request);
      trip.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "Trip", trip.Id.ToString(), oldStatus, trip.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "trips", "Edit", trip.Id.ToString(), $"Updated trip {trip.TripNumber}.");
      await db.SaveChangesAsync();
      if (!string.Equals(oldDriver, trip.DriverName, StringComparison.OrdinalIgnoreCase))
      {
        await SendTripAssignmentEmailAsync(db, emailSender, trip, changes);
      }
      else
      {
        await SendTripUpdatedEmailAsync(db, emailSender, trip, httpRequest, changes);
      }
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
    if (request.LoadWeightKg <= 0) return "Load weight must be greater than 0 kg.";
    if (request.LoadVolumeM3 < 0) return "Load volume cannot be negative.";
    if (request.PlannedDistanceKm <= 0) return "Planned distance must be greater than 0 km.";
    if (request.StartingOdometerKm < 0 || request.CurrentOdometerKm < 0) return "Odometer values cannot be negative.";
    if (request.EndingOdometerKm.HasValue && request.EndingOdometerKm.Value < 0) return "Ending odometer cannot be negative.";
    if (request.FuelIssuedLiters < 0) return "Fuel issued cannot be negative.";
    if (request.TollEstimate < 0) return "Toll estimate cannot be negative.";
    if (DateTime.TryParse(request.DepartureDateTime, out var departure) &&
        DateTime.TryParse(request.EstimatedArrival, out var eta) &&
        eta < departure)
    {
      return "Estimated arrival cannot be earlier than departure.";
    }

    if (!DateTime.TryParse(request.DepartureDateTime, out _)) return "Departure date and time is invalid.";
    if (!DateTime.TryParse(request.EstimatedArrival, out _)) return "Estimated arrival is invalid.";
    if (!string.IsNullOrWhiteSpace(request.ActualArrival) && !DateTime.TryParse(request.ActualArrival, out _)) return "Actual arrival is invalid.";
    return null;
  }

  private static async Task<string?> ValidateVehicleDriverAssignmentAsync(FleetDbContext db, TripRequest request)
  {
    var vehicleId = request.VehicleId!.Trim();
    var driverName = request.DriverName!.Trim();
    var vehicle = await db.Vehicles
      .AsNoTracking()
      .FirstOrDefaultAsync(item => item.IsDeleted == 0 && item.Id == vehicleId);

    if (vehicle is null) return "Selected vehicle could not be found.";

    if (string.IsNullOrWhiteSpace(vehicle.Driver))
    {
      return "Selected vehicle has no assigned driver. Assign a driver to the vehicle before creating a trip.";
    }

    if (!string.Equals(vehicle.Driver.Trim(), driverName, StringComparison.OrdinalIgnoreCase))
    {
      return "Selected driver is not assigned to the selected vehicle.";
    }

    var capacity = ParseVehicleCapacity(vehicle.Capacity);
    if (capacity.WeightKg.HasValue && request.LoadWeightKg > capacity.WeightKg.Value)
    {
      return $"Load weight {request.LoadWeightKg:0.##} kg is higher than {vehicle.Id}'s capacity ({vehicle.Capacity}). Choose another vehicle.";
    }

    if (capacity.VolumeM3.HasValue && request.LoadVolumeM3 > capacity.VolumeM3.Value)
    {
      return $"Load volume {request.LoadVolumeM3:0.##} m3 is higher than {vehicle.Id}'s capacity ({vehicle.Capacity}). Choose another vehicle.";
    }

    return null;
  }

  private static VehicleCapacity ParseVehicleCapacity(string? capacity)
  {
    var text = capacity?.Trim() ?? string.Empty;
    if (text.Length == 0) return new VehicleCapacity(null, null);

    var tonsMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(?:tons?|tonnes?|t)\b", RegexOptions.IgnoreCase);
    var kgMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(?:kg|kgs|kilograms?)\b", RegexOptions.IgnoreCase);
    var volumeMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(?:m3|m³|cbm|cubic\s*meters?)\b", RegexOptions.IgnoreCase);
    var numberMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);

    decimal? weightKg = null;
    if (tonsMatch.Success && decimal.TryParse(tonsMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var tons))
    {
      weightKg = tons * 1000;
    }
    else if (kgMatch.Success && decimal.TryParse(kgMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var kg))
    {
      weightKg = kg;
    }
    else if (numberMatch.Success && decimal.TryParse(numberMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
    {
      weightKg = number;
    }

    decimal? volumeM3 = null;
    if (volumeMatch.Success && decimal.TryParse(volumeMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var volume))
    {
      volumeM3 = volume;
    }

    return new VehicleCapacity(weightKg, volumeM3);
  }

  private sealed record VehicleCapacity(decimal? WeightKg, decimal? VolumeM3);

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

  private static async Task SendTripAssignmentEmailAsync(FleetDbContext db, IEmailSender emailSender, Trip trip, IReadOnlyList<string>? changes = null)
  {
    var driver = await FindAssignedDriverAsync(db, trip.DriverName);

    if (driver is null || string.IsNullOrWhiteSpace(driver.Email)) return;

    QueueEmail(
      emailSender,
      driver.Email,
      $"New trip assignment: {trip.TripNumber}",
      EmailTemplates.TripAssignment(driver, trip, changes),
      isHtml: true);
  }

  private static async Task SendTripUpdatedEmailAsync(FleetDbContext db, IEmailSender emailSender, Trip trip, HttpRequest request, IReadOnlyList<string> changes)
  {
    var driver = await FindAssignedDriverAsync(db, trip.DriverName);
    if (driver is null || string.IsNullOrWhiteSpace(driver.Email)) return;

    var updatedBy = request.Headers["X-Fleet-User-Name"].FirstOrDefault();
    var updatedByRole = AuditLogWriter.GetRequestRoleId(request);
    var updater = string.IsNullOrWhiteSpace(updatedBy)
      ? updatedByRole
      : $"{updatedBy} ({updatedByRole})";

    QueueEmail(
      emailSender,
      driver.Email,
      $"Trip updated: {trip.TripNumber}",
      EmailTemplates.TripUpdated(driver, trip, updater, changes),
      isHtml: true);
  }

  private static void QueueEmail(IEmailSender emailSender, string to, string subject, string body, bool isHtml = false)
  {
    _ = Task.Run(async () =>
    {
      try
      {
        await emailSender.SendAsync(to, subject, body, isHtml);
      }
      catch
      {
        // Trip saves should not be blocked by temporary email delivery failures.
      }
    });
  }

  private static IReadOnlyList<string> BuildTripChanges(Trip trip, TripRequest request)
  {
    var changes = new List<string>();
    AddChange(changes, "Trip number", trip.TripNumber, request.TripNumber);
    AddChange(changes, "Trip type", trip.TripType, request.TripType);
    AddChange(changes, "Status", trip.Status, request.Status);
    AddChange(changes, "Priority", trip.Priority, request.Priority);
    AddChange(changes, "Customer", trip.CustomerName, request.CustomerName);
    AddChange(changes, "Department", trip.Department, request.Department);
    AddChange(changes, "Cost center", trip.CostCenter, request.CostCenter);
    AddChange(changes, "Vehicle", $"{trip.VehiclePlate} ({trip.VehicleId})", $"{request.VehiclePlate} ({request.VehicleId})");
    AddChange(changes, "Trailer number", trip.TrailerNumber, request.TrailerNumber);
    AddChange(changes, "Driver", trip.DriverName, request.DriverName);
    AddChange(changes, "Co-driver", trip.CoDriverName, request.CoDriverName);
    AddChange(changes, "Dispatcher", trip.DispatcherName, request.DispatcherName);
    AddChange(changes, "Cargo type", trip.CargoType, request.CargoType);
    AddChange(changes, "Load weight", FormatNumber(trip.LoadWeightKg, "kg"), FormatNumber(request.LoadWeightKg, "kg"));
    AddChange(changes, "Load volume", FormatNumber(trip.LoadVolumeM3, "m3"), FormatNumber(request.LoadVolumeM3, "m3"));
    AddChange(changes, "Pickup", trip.PickupLocation, request.PickupLocation);
    AddChange(changes, "Dropoff", trip.DropoffLocation, request.DropoffLocation);
    AddChange(changes, "Pickup contact", trip.PickupContact, request.PickupContact);
    AddChange(changes, "Dropoff contact", trip.DropoffContact, request.DropoffContact);
    AddChange(changes, "Departure", FormatTripDateTime(trip.DepartureDateTime), FormatTripDateTime(request.DepartureDateTime));
    AddChange(changes, "ETA", FormatTripDateTime(trip.EstimatedArrival), FormatTripDateTime(request.EstimatedArrival));
    AddChange(changes, "Actual arrival", FormatTripDateTime(trip.ActualArrival), FormatTripDateTime(request.ActualArrival));
    AddChange(changes, "Planned distance", FormatNumber(trip.PlannedDistanceKm, "km"), FormatNumber(request.PlannedDistanceKm, "km"));
    AddChange(changes, "Start odometer", FormatNumber(trip.StartingOdometerKm, "km"), FormatNumber(request.StartingOdometerKm, "km"));
    AddChange(changes, "Current odometer", FormatNumber(trip.CurrentOdometerKm, "km"), FormatNumber(request.CurrentOdometerKm, "km"));
    AddChange(changes, "End odometer", FormatNumber(trip.EndingOdometerKm, "km"), FormatNumber(request.EndingOdometerKm, "km"));
    AddChange(changes, "Fuel issued", FormatNumber(trip.FuelIssuedLiters, "L"), FormatNumber(request.FuelIssuedLiters, "L"));
    AddChange(changes, "Toll estimate", FormatNumber(trip.TollEstimate, "MMK"), FormatNumber(request.TollEstimate, "MMK"));
    AddChange(changes, "Permit required", FormatBoolean(trip.PermitRequired), FormatBoolean(request.PermitRequired));
    AddChange(changes, "Temperature controlled", FormatBoolean(trip.TemperatureControlled), FormatBoolean(request.TemperatureControlled));
    AddChange(changes, "Temperature range", trip.TemperatureRange, request.TemperatureRange);
    AddChange(changes, "Special instructions", trip.SpecialInstructions, request.SpecialInstructions);
    AddChange(changes, "Driver notes", trip.DriverNotes, request.DriverNotes);
    return changes;
  }

  private static void AddChange(List<string> changes, string label, string? oldValue, string? newValue)
  {
    var oldText = FormatText(oldValue);
    var newText = FormatText(newValue);
    if (string.Equals(oldText, newText, StringComparison.OrdinalIgnoreCase)) return;
    changes.Add($"- {label}: {oldText} -> {newText}");
  }

  private static string FormatText(string? value) =>
    string.IsNullOrWhiteSpace(value) ? "Not set" : value.Trim();

  private static string FormatNumber(decimal? value, string unit) =>
    value.HasValue ? $"{value.Value:0.##} {unit}" : "Not set";

  private static string FormatNumber(int? value, string unit) =>
    value.HasValue ? $"{value.Value:0} {unit}" : "Not set";

  private static string FormatBoolean(bool value) =>
    value ? "Yes" : "No";

  private static Task<User?> FindAssignedDriverAsync(FleetDbContext db, string driverName) =>
    db.Users
      .Include(user => user.Role)
      .AsNoTracking()
      .FirstOrDefaultAsync(user =>
        user.IsDeleted == 0 &&
        user.Status == "Active" &&
        user.Name == driverName &&
        user.Role != null &&
        user.RoleId == "driver");

  private static string FormatTripDateTime(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return "Not set";

    if (!DateTime.TryParse(value, out var parsed))
    {
      return value.Trim();
    }

    var localTime = parsed.Kind == DateTimeKind.Utc
      ? TimeZoneInfo.ConvertTimeFromUtc(parsed, MyanmarTimeZone)
      : parsed;

    return localTime.ToString("MMM d, yyyy h:mm tt") + " Myanmar Time";
  }

  private static readonly TimeZoneInfo MyanmarTimeZone = ResolveMyanmarTimeZone();

  private static TimeZoneInfo ResolveMyanmarTimeZone()
  {
    try
    {
      return TimeZoneInfo.FindSystemTimeZoneById("Asia/Rangoon");
    }
    catch (TimeZoneNotFoundException)
    {
      return TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time");
    }
  }
}
