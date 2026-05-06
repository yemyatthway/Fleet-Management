using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class SetupOptionEndpoints
{
  public static IEndpointRouteBuilder MapSetupOptionEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapLocationTypeEndpoints();
    app.MapVehicleTypeEndpoints();
    app.MapFuelTypeEndpoints();
    return app;
  }

  private static void MapLocationTypeEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/location-types", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.LocationTypeCodeOptions.AsNoTracking().AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(locationType =>
          locationType.Name.ToLower().Contains(normalizedSearch) ||
          locationType.Code.ToLower().Contains(normalizedSearch) ||
          (locationType.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          locationType.Status.ToLower().Contains(normalizedSearch));
      }

      query = sortOrder?.ToLowerInvariant() == "desc"
        ? query.OrderByDescending(locationType => locationType.Id)
        : query.OrderBy(locationType => locationType.Id);

      var total = await query.CountAsync();
      var items = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Select(locationType => new LocationTypeDto(
          locationType.Id,
          locationType.Name,
          locationType.Code,
          locationType.Description,
          locationType.Status,
          locationType.CreatedAt,
          locationType.UpdatedAt))
        .ToListAsync();

      return Results.Ok(new PagedResult<LocationTypeDto>(items, total));
    });

    app.MapGet("/api/location-types/options", async (FleetDbContext db) =>
    {
      var items = await db.LocationTypeCodeOptions
        .AsNoTracking()
        .Where(locationType => locationType.Status == "Active")
        .OrderBy(locationType => locationType.Id)
        .Select(locationType => locationType.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    app.MapPost("/api/location-types", async (LocationTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "location-type-setup", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateLocationTypeRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.LocationTypeCodeOptions.AnyAsync(locationType =>
        locationType.Name.ToLower() == normalizedName.ToLower() ||
        locationType.Code.ToLower() == normalizedCode.ToLower());
      if (duplicateExists) return Results.BadRequest(new ApiError("Location type name or code already exists."));

      var now = DateTimeOffset.UtcNow;
      var locationType = new LocationTypeCodeOption
      {
        Name = normalizedName,
        Code = normalizedCode,
        Description = NormalizeOptional(request.Description),
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
        CreatedAt = now,
        UpdatedAt = now
      };

      db.LocationTypeCodeOptions.Add(locationType);
      await db.SaveChangesAsync();

      return Results.Ok(ToLocationTypeDto(locationType));
    });

    app.MapPut("/api/location-types/{id:int}", async (int id, LocationTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "location-type-setup", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateLocationTypeRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var locationType = await db.LocationTypeCodeOptions.FindAsync(id);
      if (locationType is null) return Results.NotFound(new ApiError("Location type not found."));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.LocationTypeCodeOptions.AnyAsync(item =>
        item.Id != id &&
        (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
      if (duplicateExists) return Results.BadRequest(new ApiError("Location type name or code already exists."));

      locationType.Name = normalizedName;
      locationType.Code = normalizedCode;
      locationType.Description = NormalizeOptional(request.Description);
      locationType.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
      locationType.UpdatedAt = DateTimeOffset.UtcNow;

      await db.SaveChangesAsync();

      return Results.Ok(ToLocationTypeDto(locationType));
    });

    app.MapDelete("/api/location-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "location-type-setup", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var locationType = await db.LocationTypeCodeOptions.FindAsync(id);
      if (locationType is null) return Results.NotFound(new ApiError("Location type not found."));

      db.LocationTypeCodeOptions.Remove(locationType);
      await db.SaveChangesAsync();
      return Results.NoContent();
    });
  }

  private static void MapVehicleTypeEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/vehicle-types", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.VehicleTypeCodeOptions.AsNoTracking().AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(vehicleType =>
          vehicleType.Name.ToLower().Contains(normalizedSearch) ||
          vehicleType.Code.ToLower().Contains(normalizedSearch) ||
          (vehicleType.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          vehicleType.Status.ToLower().Contains(normalizedSearch));
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("id", "desc") => query.OrderByDescending(vehicleType => vehicleType.Id),
        ("id", _) => query.OrderBy(vehicleType => vehicleType.Id),
        ("code", "desc") => query.OrderByDescending(vehicleType => vehicleType.Code),
        ("code", _) => query.OrderBy(vehicleType => vehicleType.Code),
        ("description", "desc") => query.OrderByDescending(vehicleType => vehicleType.Description),
        ("description", _) => query.OrderBy(vehicleType => vehicleType.Description),
        ("status", "desc") => query.OrderByDescending(vehicleType => vehicleType.Status),
        ("status", _) => query.OrderBy(vehicleType => vehicleType.Status),
        ("createdat", "desc") => query.OrderByDescending(vehicleType => vehicleType.CreatedAt),
        ("createdat", _) => query.OrderBy(vehicleType => vehicleType.CreatedAt),
        ("updatedat", "desc") => query.OrderByDescending(vehicleType => vehicleType.UpdatedAt),
        ("updatedat", _) => query.OrderBy(vehicleType => vehicleType.UpdatedAt),
        ("name", "desc") => query.OrderByDescending(vehicleType => vehicleType.Name),
        ("name", _) => query.OrderBy(vehicleType => vehicleType.Name),
        _ => query.OrderBy(vehicleType => vehicleType.Id)
      };

      var total = await query.CountAsync();
      var items = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Select(vehicleType => new VehicleTypeDto(
          vehicleType.Id,
          vehicleType.Name,
          vehicleType.Code,
          vehicleType.Description,
          vehicleType.Status,
          vehicleType.CreatedAt,
          vehicleType.UpdatedAt))
        .ToListAsync();

      return Results.Ok(new PagedResult<VehicleTypeDto>(items, total));
    });

    app.MapGet("/api/vehicle-types/options", async (FleetDbContext db) =>
    {
      var items = await db.VehicleTypeCodeOptions
        .AsNoTracking()
        .Where(vehicleType => vehicleType.Status == "Active")
        .OrderBy(vehicleType => vehicleType.Name)
        .Select(vehicleType => vehicleType.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    app.MapPost("/api/vehicle-types", async (VehicleTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "vehicle-type-setup", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateVehicleTypeRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.VehicleTypeCodeOptions.AnyAsync(vehicleType =>
        vehicleType.Name.ToLower() == normalizedName.ToLower() ||
        vehicleType.Code.ToLower() == normalizedCode.ToLower());
      if (duplicateExists) return Results.BadRequest(new ApiError("Vehicle type name or code already exists."));

      var now = DateTimeOffset.UtcNow;
      var vehicleType = new VehicleTypeCodeOption
      {
        Name = normalizedName,
        Code = normalizedCode,
        Description = NormalizeOptional(request.Description),
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
        CreatedAt = now,
        UpdatedAt = now
      };

      db.VehicleTypeCodeOptions.Add(vehicleType);
      await db.SaveChangesAsync();

      return Results.Ok(ToVehicleTypeDto(vehicleType));
    });

    app.MapPut("/api/vehicle-types/{id:int}", async (int id, VehicleTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "vehicle-type-setup", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateVehicleTypeRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var vehicleType = await db.VehicleTypeCodeOptions.FindAsync(id);
      if (vehicleType is null) return Results.NotFound(new ApiError("Vehicle type not found."));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.VehicleTypeCodeOptions.AnyAsync(item =>
        item.Id != id &&
        (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
      if (duplicateExists) return Results.BadRequest(new ApiError("Vehicle type name or code already exists."));

      vehicleType.Name = normalizedName;
      vehicleType.Code = normalizedCode;
      vehicleType.Description = NormalizeOptional(request.Description);
      vehicleType.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
      vehicleType.UpdatedAt = DateTimeOffset.UtcNow;

      await db.SaveChangesAsync();

      return Results.Ok(ToVehicleTypeDto(vehicleType));
    });

    app.MapDelete("/api/vehicle-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "vehicle-type-setup", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var vehicleType = await db.VehicleTypeCodeOptions.FindAsync(id);
      if (vehicleType is null) return Results.NotFound(new ApiError("Vehicle type not found."));

      db.VehicleTypeCodeOptions.Remove(vehicleType);
      await db.SaveChangesAsync();
      return Results.NoContent();
    });
  }

  private static void MapFuelTypeEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/fuel-types", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.FuelTypeCodeOptions.AsNoTracking().AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(fuelType =>
          fuelType.Name.ToLower().Contains(normalizedSearch) ||
          fuelType.Code.ToLower().Contains(normalizedSearch) ||
          (fuelType.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          fuelType.Status.ToLower().Contains(normalizedSearch));
      }

      query = sortOrder?.ToLowerInvariant() == "desc"
        ? query.OrderByDescending(fuelType => fuelType.Id)
        : query.OrderBy(fuelType => fuelType.Id);

      var total = await query.CountAsync();
      var items = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Select(fuelType => new FuelTypeDto(
          fuelType.Id,
          fuelType.Name,
          fuelType.Code,
          fuelType.Description,
          fuelType.Status,
          fuelType.CreatedAt,
          fuelType.UpdatedAt))
        .ToListAsync();

      return Results.Ok(new PagedResult<FuelTypeDto>(items, total));
    });

    app.MapGet("/api/fuel-types/options", async (FleetDbContext db) =>
    {
      var items = await db.FuelTypeCodeOptions
        .AsNoTracking()
        .Where(fuelType => fuelType.Status == "Active")
        .OrderBy(fuelType => fuelType.Id)
        .Select(fuelType => fuelType.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    app.MapPost("/api/fuel-types", async (FuelTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "fuel-type-setup", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateFuelTypeRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.FuelTypeCodeOptions.AnyAsync(fuelType =>
        fuelType.Name.ToLower() == normalizedName.ToLower() ||
        fuelType.Code.ToLower() == normalizedCode.ToLower());
      if (duplicateExists) return Results.BadRequest(new ApiError("Fuel type name or code already exists."));

      var now = DateTimeOffset.UtcNow;
      var fuelType = new FuelTypeCodeOption
      {
        Name = normalizedName,
        Code = normalizedCode,
        Description = NormalizeOptional(request.Description),
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
        CreatedAt = now,
        UpdatedAt = now
      };

      db.FuelTypeCodeOptions.Add(fuelType);
      await db.SaveChangesAsync();

      return Results.Ok(ToFuelTypeDto(fuelType));
    });

    app.MapPut("/api/fuel-types/{id:int}", async (int id, FuelTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "fuel-type-setup", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateFuelTypeRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var fuelType = await db.FuelTypeCodeOptions.FindAsync(id);
      if (fuelType is null) return Results.NotFound(new ApiError("Fuel type not found."));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.FuelTypeCodeOptions.AnyAsync(item =>
        item.Id != id &&
        (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
      if (duplicateExists) return Results.BadRequest(new ApiError("Fuel type name or code already exists."));

      fuelType.Name = normalizedName;
      fuelType.Code = normalizedCode;
      fuelType.Description = NormalizeOptional(request.Description);
      fuelType.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
      fuelType.UpdatedAt = DateTimeOffset.UtcNow;

      await db.SaveChangesAsync();

      return Results.Ok(ToFuelTypeDto(fuelType));
    });

    app.MapDelete("/api/fuel-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "fuel-type-setup", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var fuelType = await db.FuelTypeCodeOptions.FindAsync(id);
      if (fuelType is null) return Results.NotFound(new ApiError("Fuel type not found."));

      db.FuelTypeCodeOptions.Remove(fuelType);
      await db.SaveChangesAsync();
      return Results.NoContent();
    });
  }

  private static LocationTypeDto ToLocationTypeDto(LocationTypeCodeOption locationType) =>
    new(
      locationType.Id,
      locationType.Name,
      locationType.Code,
      locationType.Description,
      locationType.Status,
      locationType.CreatedAt,
      locationType.UpdatedAt);

  private static VehicleTypeDto ToVehicleTypeDto(VehicleTypeCodeOption vehicleType) =>
    new(
      vehicleType.Id,
      vehicleType.Name,
      vehicleType.Code,
      vehicleType.Description,
      vehicleType.Status,
      vehicleType.CreatedAt,
      vehicleType.UpdatedAt);

  private static FuelTypeDto ToFuelTypeDto(FuelTypeCodeOption fuelType) =>
    new(
      fuelType.Id,
      fuelType.Name,
      fuelType.Code,
      fuelType.Description,
      fuelType.Status,
      fuelType.CreatedAt,
      fuelType.UpdatedAt);

  private static string? ValidateLocationTypeRequest(LocationTypeRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Location type name is required.";
    if (request.Name.Trim().Length > 120) return "Location type name must be 120 characters or fewer.";
    if (string.IsNullOrWhiteSpace(request.Code)) return "Location type code is required.";
    if (request.Code.Trim().Length > 40) return "Location type code must be 40 characters or fewer.";
    if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
    {
      return "Location type description must be 500 characters or fewer.";
    }

    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
    return normalizedStatus is "Active" or "Disabled"
      ? null
      : "Location type status must be Active or Disabled.";
  }

  private static string? ValidateVehicleTypeRequest(VehicleTypeRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Vehicle type name is required.";
    if (request.Name.Trim().Length > 120) return "Vehicle type name must be 120 characters or fewer.";
    if (string.IsNullOrWhiteSpace(request.Code)) return "Vehicle type code is required.";
    if (request.Code.Trim().Length > 40) return "Vehicle type code must be 40 characters or fewer.";
    if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
    {
      return "Vehicle type description must be 500 characters or fewer.";
    }

    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
    return normalizedStatus is "Active" or "Disabled"
      ? null
      : "Vehicle type status must be Active or Disabled.";
  }

  private static string? ValidateFuelTypeRequest(FuelTypeRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Fuel type name is required.";
    if (request.Name.Trim().Length > 120) return "Fuel type name must be 120 characters or fewer.";
    if (string.IsNullOrWhiteSpace(request.Code)) return "Fuel type code is required.";
    if (request.Code.Trim().Length > 40) return "Fuel type code must be 40 characters or fewer.";
    if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
    {
      return "Fuel type description must be 500 characters or fewer.";
    }

    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
    return normalizedStatus is "Active" or "Disabled"
      ? null
      : "Fuel type status must be Active or Disabled.";
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
