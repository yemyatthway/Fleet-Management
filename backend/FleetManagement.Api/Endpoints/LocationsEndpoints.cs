using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class LocationsEndpoints
{
  public static IEndpointRouteBuilder MapLocationsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/locations", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.LocationCodeOptions.AsNoTracking().AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(location =>
          location.Name.ToLower().Contains(normalizedSearch) ||
          location.Code.ToLower().Contains(normalizedSearch) ||
          location.Type.ToLower().Contains(normalizedSearch) ||
          location.Address.ToLower().Contains(normalizedSearch) ||
          location.City.ToLower().Contains(normalizedSearch) ||
          location.Country.ToLower().Contains(normalizedSearch) ||
          location.Phone.ToLower().Contains(normalizedSearch) ||
          (location.ContactPerson ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          (location.Notes ?? string.Empty).ToLower().Contains(normalizedSearch));
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("id", "desc") => query.OrderByDescending(location => location.Id),
        ("id", _) => query.OrderBy(location => location.Id),
        ("code", "desc") => query.OrderByDescending(location => location.Code),
        ("code", _) => query.OrderBy(location => location.Code),
        ("type", "desc") => query.OrderByDescending(location => location.Type),
        ("type", _) => query.OrderBy(location => location.Type),
        ("city", "desc") => query.OrderByDescending(location => location.City),
        ("city", _) => query.OrderBy(location => location.City),
        ("country", "desc") => query.OrderByDescending(location => location.Country),
        ("country", _) => query.OrderBy(location => location.Country),
        ("status", "desc") => query.OrderByDescending(location => location.Status),
        ("status", _) => query.OrderBy(location => location.Status),
        ("createdat", "desc") => query.OrderByDescending(location => location.CreatedAt),
        ("createdat", _) => query.OrderBy(location => location.CreatedAt),
        ("updatedat", "desc") => query.OrderByDescending(location => location.UpdatedAt),
        ("updatedat", _) => query.OrderBy(location => location.UpdatedAt),
        ("name", "desc") => query.OrderByDescending(location => location.Name),
        ("name", _) => query.OrderBy(location => location.Name),
        _ => query.OrderBy(location => location.Id)
      };

      var total = await query.CountAsync();
      var items = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Select(location => new LocationDto(
          location.Id,
          location.Name,
          location.Code,
          location.Type,
          location.Address,
          location.City,
          location.Country,
          location.ContactPerson,
          location.Phone,
          location.OperatingHours,
          location.Notes,
          location.Status,
          location.CreatedAt,
          location.UpdatedAt))
        .ToListAsync();

      return Results.Ok(new PagedResult<LocationDto>(items, total));
    });

    app.MapGet("/api/locations/options", async (FleetDbContext db) =>
    {
      var items = await db.LocationCodeOptions
        .AsNoTracking()
        .Where(location => location.Status == "Active")
        .OrderBy(location => location.Name)
        .Select(location => location.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    app.MapPost("/api/locations", async (LocationRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "location-setup", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateLocationRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.LocationCodeOptions.AnyAsync(location =>
        location.Name.ToLower() == normalizedName.ToLower() ||
        location.Code.ToLower() == normalizedCode.ToLower());
      if (duplicateExists) return Results.BadRequest(new ApiError("Location name or code already exists."));

      var location = new LocationCodeOption
      {
        Name = normalizedName,
        Code = normalizedCode,
        Type = request.Type.Trim(),
        Address = request.Address.Trim(),
        City = request.City.Trim(),
        Country = request.Country.Trim(),
        ContactPerson = NormalizeOptional(request.ContactPerson),
        Phone = request.Phone.Trim(),
        OperatingHours = request.OperatingHours.Trim(),
        Notes = NormalizeOptional(request.Notes),
        Status = request.Status.Trim(),
        CreatedAt = DateTimeOffset.UtcNow
      };

      db.LocationCodeOptions.Add(location);
      await db.SaveChangesAsync();

      return Results.Ok(ToLocationDto(location));
    });

    app.MapPut("/api/locations/{id:int}", async (int id, LocationRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "location-setup", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateLocationRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var location = await db.LocationCodeOptions.FindAsync(id);
      if (location is null) return Results.NotFound(new ApiError("Location not found."));

      var normalizedName = request.Name.Trim();
      var normalizedCode = request.Code.Trim();
      var duplicateExists = await db.LocationCodeOptions.AnyAsync(item =>
        item.Id != id &&
        (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
      if (duplicateExists) return Results.BadRequest(new ApiError("Location name or code already exists."));

      location.Name = normalizedName;
      location.Code = normalizedCode;
      location.Type = request.Type.Trim();
      location.Address = request.Address.Trim();
      location.City = request.City.Trim();
      location.Country = request.Country.Trim();
      location.ContactPerson = NormalizeOptional(request.ContactPerson);
      location.Phone = request.Phone.Trim();
      location.OperatingHours = request.OperatingHours.Trim();
      location.Notes = NormalizeOptional(request.Notes);
      location.Status = request.Status.Trim();
      location.UpdatedAt = DateTimeOffset.UtcNow;

      await db.SaveChangesAsync();

      return Results.Ok(ToLocationDto(location));
    });

    app.MapDelete("/api/locations/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "location-setup", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var location = await db.LocationCodeOptions.FindAsync(id);
      if (location is null) return Results.NotFound(new ApiError("Location not found."));

      db.LocationCodeOptions.Remove(location);
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static LocationDto ToLocationDto(LocationCodeOption location) =>
    new(
      location.Id,
      location.Name,
      location.Code,
      location.Type,
      location.Address,
      location.City,
      location.Country,
      location.ContactPerson,
      location.Phone,
      location.OperatingHours,
      location.Notes,
      location.Status,
      location.CreatedAt,
      location.UpdatedAt);

  private static string? ValidateLocationRequest(LocationRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Location name is required.";
    if (string.IsNullOrWhiteSpace(request.Code)) return "Location code is required.";
    if (string.IsNullOrWhiteSpace(request.Type)) return "Location type is required.";
    if (string.IsNullOrWhiteSpace(request.Address)) return "Location address is required.";
    if (string.IsNullOrWhiteSpace(request.City)) return "Location city is required.";
    if (string.IsNullOrWhiteSpace(request.Country)) return "Location country is required.";
    if (string.IsNullOrWhiteSpace(request.Phone)) return "Location phone is required.";
    if (string.IsNullOrWhiteSpace(request.OperatingHours)) return "Operating hours are required.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
    return null;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
