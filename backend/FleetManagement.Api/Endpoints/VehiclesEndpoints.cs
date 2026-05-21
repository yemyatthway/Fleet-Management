using FleetManagement.Api.Assets;
using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class VehiclesEndpoints
{
  public static IEndpointRouteBuilder MapVehiclesEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/vehicles", async (
      HttpRequest request,
      FleetDbContext db,
      string? search = null,
      string? status = null,
      string? scope = null) =>
    {
      var query = db.Vehicles
        .Where(vehicle => vehicle.IsDeleted == 0)
        .AsNoTracking()
        .AsQueryable();
      var roleId = AuditLogWriter.GetRequestRoleId(request);
      var userName = request.Headers["X-Fleet-User-Name"].FirstOrDefault();
      var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "mine" : scope.Trim().ToLowerInvariant();
      if (normalizedScope != "all" && string.Equals(roleId, "driver", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(userName))
      {
        var normalizedUserName = userName.Trim().ToLower();
        query = query.Where(vehicle => vehicle.Driver.ToLower() == normalizedUserName);
      }

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(vehicle =>
          vehicle.Id.ToLower().Contains(normalizedSearch) ||
          vehicle.Plate.ToLower().Contains(normalizedSearch) ||
          vehicle.Driver.ToLower().Contains(normalizedSearch) ||
          vehicle.Type.ToLower().Contains(normalizedSearch) ||
          vehicle.Model.ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(status) && status != "All")
      {
        var normalizedStatus = status.Trim().ToLower();
        query = query.Where(vehicle => vehicle.Status.ToLower() == normalizedStatus);
      }

      var records = await query
        .OrderBy(vehicle => vehicle.Id)
        .ToListAsync();

      return Results.Ok(records.Select(vehicle => ToVehicleDto(vehicle, request)).ToList());
    });

    app.MapPost("/api/vehicles", async (
      [FromForm] VehicleFormData form,
      HttpRequest request,
      IWebHostEnvironment environment,
      FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(request, db, "vehicles", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateVehicleRequest(form);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var vehicleImageError = UserAssetStorage.ValidateImageFile(form.VehicleImageFile);
      if (vehicleImageError is not null) return Results.BadRequest(new ApiError(vehicleImageError));

      var driverImageError = UserAssetStorage.ValidateImageFile(form.DriverImageFile);
      if (driverImageError is not null) return Results.BadRequest(new ApiError(driverImageError));

      var normalizedPlate = form.Plate!.Trim().ToUpperInvariant();
      var duplicatePlate = await db.Vehicles.AnyAsync(vehicle =>
        vehicle.IsDeleted == 0 && vehicle.Plate.ToLower() == normalizedPlate.ToLower());
      if (duplicatePlate) return Results.BadRequest(new ApiError("Vehicle plate already exists."));

      var now = DateTime.UtcNow;
      var vehicle = new Vehicle
      {
        Id = NextVehicleId(await db.Vehicles.Select(item => item.Id).ToListAsync()),
        Plate = normalizedPlate,
        Region = form.Region!.Trim(),
        Type = form.Type!.Trim(),
        Model = form.Model!.Trim(),
        Make = NormalizeOptional(form.Make),
        Year = NormalizeOptional(form.Year),
        Color = NormalizeOptional(form.Color),
        Status = form.Status!.Trim(),
        Ownership = NormalizeOptional(form.Ownership) ?? "Owned",
        Driver = form.Driver!.Trim(),
        DriverImage = string.Empty,
        Depot = NormalizeOptional(form.Depot),
        Capacity = NormalizeOptional(form.Capacity),
        FuelCapacity = NormalizeOptional(form.FuelCapacity),
        FuelType = form.FuelType!.Trim(),
        Vin = NormalizeOptional(form.Vin),
        EngineNo = NormalizeOptional(form.EngineNo),
        Odometer = NormalizeOptional(form.Odometer),
        LastService = NormalizeOptional(form.LastService),
        NextService = NormalizeOptional(form.NextService),
        ServiceNote = NormalizeOptional(form.ServiceNote),
        PurchaseCost = NormalizeOptional(form.PurchaseCost),
        RegistrationNo = NormalizeOptional(form.RegistrationNo),
        RegistrationExpiry = NormalizeOptional(form.RegistrationExpiry),
        RoadTaxExpiry = NormalizeOptional(form.RoadTaxExpiry),
        InsuranceExpiry = NormalizeOptional(form.InsuranceExpiry),
        InsuranceProvider = NormalizeOptional(form.InsuranceProvider),
        InsurancePolicy = NormalizeOptional(form.InsurancePolicy),
        InspectionDue = NormalizeOptional(form.InspectionDue),
        AcquiredDate = NormalizeOptional(form.AcquiredDate),
        Image = string.Empty,
        IsDeleted = 0,
        CreatedAt = now,
        UpdatedAt = now
      };

      db.Vehicles.Add(vehicle);
      await db.SaveChangesAsync();

      if (form.DriverImageFile is null && !form.RemoveDriverImage)
      {
        vehicle.DriverImage = await FindDriverAvatarAsync(db, vehicle.Driver);
      }

      if (form.VehicleImageFile is not null)
      {
        vehicle.Image = await UserAssetStorage.SaveImageAsync(form.VehicleImageFile, "vehicles", vehicle.Id, "vehicle-image", environment);
      }

      if (form.DriverImageFile is not null)
      {
        vehicle.DriverImage = await UserAssetStorage.SaveImageAsync(form.DriverImageFile, "vehicles", vehicle.Id, "driver-image", environment);
      }

      vehicle.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, request, "vehicles", "Create", vehicle.Id, $"Created vehicle {vehicle.Id}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToVehicleDto(vehicle, request));
    }).DisableAntiforgery();

    app.MapPut("/api/vehicles/{vehicleId}", async (
      string vehicleId,
      [FromForm] VehicleFormData form,
      HttpRequest request,
      IWebHostEnvironment environment,
      FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(request, db, "vehicles", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateVehicleRequest(form);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var vehicleImageError = UserAssetStorage.ValidateImageFile(form.VehicleImageFile);
      if (vehicleImageError is not null) return Results.BadRequest(new ApiError(vehicleImageError));

      var driverImageError = UserAssetStorage.ValidateImageFile(form.DriverImageFile);
      if (driverImageError is not null) return Results.BadRequest(new ApiError(driverImageError));

      var vehicle = await db.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId && item.IsDeleted == 0);
      if (vehicle is null) return Results.NotFound(new ApiError("Vehicle not found."));

      var normalizedPlate = form.Plate!.Trim().ToUpperInvariant();
      var duplicatePlate = await db.Vehicles.AnyAsync(item =>
        item.Id != vehicleId &&
        item.IsDeleted == 0 &&
        item.Plate.ToLower() == normalizedPlate.ToLower());
      if (duplicatePlate) return Results.BadRequest(new ApiError("Vehicle plate already exists."));

      ApplyVehicleForm(vehicle, form, normalizedPlate);

      if (form.RemoveVehicleImage)
      {
        UserAssetStorage.DeleteStoredAsset("vehicles", vehicle.Id, vehicle.Image, environment);
        vehicle.Image = string.Empty;
      }

      if (form.RemoveDriverImage)
      {
        UserAssetStorage.DeleteStoredAsset("vehicles", vehicle.Id, vehicle.DriverImage, environment);
        vehicle.DriverImage = string.Empty;
      }

      if (form.DriverImageFile is null && !form.RemoveDriverImage)
      {
        UserAssetStorage.DeleteStoredAsset("vehicles", vehicle.Id, vehicle.DriverImage, environment);
        vehicle.DriverImage = await FindDriverAvatarAsync(db, vehicle.Driver);
      }

      if (form.VehicleImageFile is not null)
      {
        vehicle.Image = await UserAssetStorage.SaveImageAsync(form.VehicleImageFile, "vehicles", vehicle.Id, "vehicle-image", environment);
      }

      if (form.DriverImageFile is not null)
      {
        vehicle.DriverImage = await UserAssetStorage.SaveImageAsync(form.DriverImageFile, "vehicles", vehicle.Id, "driver-image", environment);
      }

      vehicle.UpdatedAt = DateTime.UtcNow;

      await AuditLogWriter.LogAuditAsync(db, request, "vehicles", "Edit", vehicle.Id, $"Updated vehicle {vehicle.Id}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToVehicleDto(vehicle, request));
    }).DisableAntiforgery();

    app.MapPatch("/api/vehicles/{vehicleId}/status", async (string vehicleId, VehicleStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "vehicles", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
      if (string.IsNullOrWhiteSpace(normalizedStatus))
      {
        return Results.BadRequest(new ApiError("Vehicle status is required."));
      }

      var vehicle = await db.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId && item.IsDeleted == 0);
      if (vehicle is null) return Results.NotFound(new ApiError("Vehicle not found."));

      var oldStatus = vehicle.Status;
      vehicle.Status = normalizedStatus;
      vehicle.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "Vehicle", vehicle.Id, oldStatus, vehicle.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "vehicles", "Edit", vehicle.Id, $"Changed vehicle status for {vehicle.Id}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToVehicleDto(vehicle, httpRequest));
    });

    app.MapDelete("/api/vehicles/{vehicleId}", async (string vehicleId, HttpRequest httpRequest, IWebHostEnvironment environment, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "vehicles", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var vehicle = await db.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId && item.IsDeleted == 0);
      if (vehicle is null) return Results.NotFound(new ApiError("Vehicle not found."));

      vehicle.IsDeleted = 1;
      vehicle.Image = string.Empty;
      vehicle.DriverImage = string.Empty;
      vehicle.UpdatedAt = DateTime.UtcNow;
      UserAssetStorage.DeleteEntityDirectory("vehicles", vehicle.Id, environment);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "vehicles", "Delete", vehicle.Id, $"Deleted vehicle {vehicle.Id}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static async Task<string?> FindDriverAvatarAsync(FleetDbContext db, string driverName)
  {
    var driverAvatar = await db.Users
      .Include(user => user.Role)
      .Where(user =>
        user.IsDeleted == 0 &&
        user.Status == "Active" &&
        user.Name == driverName &&
        user.Role != null &&
        user.Role.Name == "Driver")
      .Select(user => user.Avatar)
      .FirstOrDefaultAsync();

    return NormalizeOptional(driverAvatar);
  }

  private static void ApplyVehicleForm(Vehicle vehicle, VehicleFormData form, string normalizedPlate)
  {
    vehicle.Plate = normalizedPlate;
    vehicle.Region = form.Region!.Trim();
    vehicle.Type = form.Type!.Trim();
    vehicle.Model = form.Model!.Trim();
    vehicle.Make = NormalizeOptional(form.Make);
    vehicle.Year = NormalizeOptional(form.Year);
    vehicle.Color = NormalizeOptional(form.Color);
    vehicle.Status = form.Status!.Trim();
    vehicle.Ownership = NormalizeOptional(form.Ownership) ?? "Owned";
    vehicle.Driver = form.Driver!.Trim();
    vehicle.Depot = NormalizeOptional(form.Depot);
    vehicle.Capacity = NormalizeOptional(form.Capacity);
    vehicle.FuelCapacity = NormalizeOptional(form.FuelCapacity);
    vehicle.FuelType = form.FuelType!.Trim();
    vehicle.Vin = NormalizeOptional(form.Vin);
    vehicle.EngineNo = NormalizeOptional(form.EngineNo);
    vehicle.Odometer = NormalizeOptional(form.Odometer);
    vehicle.LastService = NormalizeOptional(form.LastService);
    vehicle.NextService = NormalizeOptional(form.NextService);
    vehicle.ServiceNote = NormalizeOptional(form.ServiceNote);
    vehicle.PurchaseCost = NormalizeOptional(form.PurchaseCost);
    vehicle.RegistrationNo = NormalizeOptional(form.RegistrationNo);
    vehicle.RegistrationExpiry = NormalizeOptional(form.RegistrationExpiry);
    vehicle.RoadTaxExpiry = NormalizeOptional(form.RoadTaxExpiry);
    vehicle.InsuranceExpiry = NormalizeOptional(form.InsuranceExpiry);
    vehicle.InsuranceProvider = NormalizeOptional(form.InsuranceProvider);
    vehicle.InsurancePolicy = NormalizeOptional(form.InsurancePolicy);
    vehicle.InspectionDue = NormalizeOptional(form.InspectionDue);
    vehicle.AcquiredDate = NormalizeOptional(form.AcquiredDate);
  }

  private static VehicleDto ToVehicleDto(Vehicle vehicle, HttpRequest request) =>
    new(
      vehicle.Id,
      vehicle.Plate,
      vehicle.Region,
      vehicle.Type,
      vehicle.Model,
      vehicle.Make,
      vehicle.Year,
      vehicle.Color,
      vehicle.Status,
      vehicle.Ownership,
      vehicle.Driver,
      PublicAssetUrls.ToPublicAssetUrl(request, vehicle.DriverImage),
      vehicle.Depot,
      vehicle.Capacity,
      vehicle.FuelCapacity,
      vehicle.FuelType,
      vehicle.Vin,
      vehicle.EngineNo,
      vehicle.Odometer,
      vehicle.LastService,
      vehicle.NextService,
      vehicle.ServiceNote,
      vehicle.PurchaseCost,
      vehicle.RegistrationNo,
      vehicle.RegistrationExpiry,
      vehicle.RoadTaxExpiry,
      vehicle.InsuranceExpiry,
      vehicle.InsuranceProvider,
      vehicle.InsurancePolicy,
      vehicle.InspectionDue,
      vehicle.AcquiredDate,
      PublicAssetUrls.ToPublicAssetUrl(request, vehicle.Image),
      vehicle.CreatedAt,
      vehicle.UpdatedAt);

  private static string NextVehicleId(IEnumerable<string> existingIds)
  {
    var max = existingIds
      .Select(value =>
      {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var normalized = value.StartsWith("VH-", StringComparison.OrdinalIgnoreCase)
          ? value[3..]
          : value;
        return int.TryParse(normalized, out var number) ? number : 0;
      })
      .DefaultIfEmpty(1000)
      .Max();

    return $"VH-{max + 1:D4}";
  }

  private static string? ValidateVehicleRequest(VehicleFormData request)
  {
    if (string.IsNullOrWhiteSpace(request.Plate)) return "Plate number is required.";
    if (request.Plate.Trim().Length > 40) return "Plate number must be 40 characters or fewer.";
    if (string.IsNullOrWhiteSpace(request.Region)) return "Region is required.";
    if (string.IsNullOrWhiteSpace(request.Type)) return "Vehicle type is required.";
    if (string.IsNullOrWhiteSpace(request.Model)) return "Vehicle model is required.";
    if (string.IsNullOrWhiteSpace(request.Driver)) return "Driver is required.";
    if (string.IsNullOrWhiteSpace(request.FuelType)) return "Fuel type is required.";

    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
    return string.IsNullOrWhiteSpace(normalizedStatus) ? "Vehicle status is required." : null;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
