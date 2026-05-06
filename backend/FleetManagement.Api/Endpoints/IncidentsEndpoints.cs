using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class IncidentsEndpoints
{
  public static IEndpointRouteBuilder MapIncidentsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/incidents", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? status = null,
      string? severity = null,
      string? sortBy = "date",
      string? sortOrder = "desc") =>
    {
      var query = db.Incidents
        .Where(incident => incident.IsDeleted == 0)
        .AsNoTracking()
        .AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(incident =>
          incident.Id.ToLower().Contains(normalizedSearch) ||
          incident.VehicleId.ToLower().Contains(normalizedSearch) ||
          incident.Driver.ToLower().Contains(normalizedSearch) ||
          incident.Type.ToLower().Contains(normalizedSearch) ||
          (incident.Notes ?? string.Empty).ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(status) && status != "All")
      {
        var normalizedStatus = status.Trim().ToLower();
        query = query.Where(incident => incident.Status.ToLower() == normalizedStatus);
      }

      if (!string.IsNullOrWhiteSpace(severity) && severity != "All")
      {
        var normalizedSeverity = severity.Trim().ToLower();
        query = query.Where(incident => incident.Severity.ToLower() == normalizedSeverity);
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("id", "asc") => query.OrderBy(incident => incident.Id),
        ("id", _) => query.OrderByDescending(incident => incident.Id),
        ("status", "asc") => query.OrderBy(incident => incident.Status),
        ("status", _) => query.OrderByDescending(incident => incident.Status),
        ("severity", "asc") => query.OrderBy(incident => incident.Severity),
        ("date", "asc") => query.OrderBy(incident => incident.Date),
        _ => query.OrderByDescending(incident => incident.Date)
      };

      var total = await query.CountAsync();
      var records = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      return Results.Ok(new PagedResult<IncidentDto>(records.Select(ToIncidentDto).ToList(), total));
    });

    app.MapPost("/api/incidents", async (IncidentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "incidents", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateIncidentRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var now = DateTime.UtcNow;
      var incident = new Incident
      {
        Id = NextIncidentId(await db.Incidents.Select(item => item.Id).ToListAsync()),
        VehicleId = request.VehicleId.Trim(),
        Driver = request.Driver.Trim(),
        Date = request.Date.Trim(),
        Type = request.Type.Trim(),
        Severity = request.Severity.Trim(),
        Status = request.Status.Trim(),
        Cost = NormalizeOptional(request.Cost),
        Notes = NormalizeOptional(request.Notes),
        IsDeleted = 0,
        CreatedAt = now,
        UpdatedAt = now
      };

      db.Incidents.Add(incident);
      await db.SaveChangesAsync();
      return Results.Ok(ToIncidentDto(incident));
    });

    app.MapPut("/api/incidents/{incidentId}", async (string incidentId, IncidentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "incidents", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateIncidentRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var incident = await db.Incidents.FirstOrDefaultAsync(item => item.Id == incidentId && item.IsDeleted == 0);
      if (incident is null) return Results.NotFound(new ApiError("Incident not found."));

      var oldStatus = incident.Status;
      incident.VehicleId = request.VehicleId.Trim();
      incident.Driver = request.Driver.Trim();
      incident.Date = request.Date.Trim();
      incident.Type = request.Type.Trim();
      incident.Severity = request.Severity.Trim();
      incident.Status = request.Status.Trim();
      incident.Cost = NormalizeOptional(request.Cost);
      incident.Notes = NormalizeOptional(request.Notes);
      incident.UpdatedAt = DateTime.UtcNow;

      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "Incident", incident.Id, oldStatus, incident.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "incidents", "Edit", incident.Id, $"Updated incident {incident.Id}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToIncidentDto(incident));
    });

    app.MapDelete("/api/incidents/{incidentId}", async (string incidentId, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "incidents", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var incident = await db.Incidents.FirstOrDefaultAsync(item => item.Id == incidentId && item.IsDeleted == 0);
      if (incident is null) return Results.NotFound(new ApiError("Incident not found."));

      incident.IsDeleted = 1;
      incident.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "incidents", "Delete", incident.Id, $"Deleted incident {incident.Id}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static IncidentDto ToIncidentDto(Incident incident) =>
    new(
      incident.Id,
      incident.VehicleId,
      incident.Driver,
      incident.Date,
      incident.Type,
      incident.Severity,
      incident.Status,
      incident.Cost,
      incident.Notes,
      incident.CreatedAt,
      incident.UpdatedAt);

  private static string NextIncidentId(IEnumerable<string> existingIds)
  {
    var max = existingIds
      .Select(value =>
      {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var normalized = value.StartsWith("INC-", StringComparison.OrdinalIgnoreCase)
          ? value[4..]
          : value;
        return int.TryParse(normalized, out var number) ? number : 0;
      })
      .DefaultIfEmpty(1000)
      .Max();

    return $"INC-{max + 1}";
  }

  private static string? ValidateIncidentRequest(IncidentRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.VehicleId)) return "Vehicle is required.";
    if (string.IsNullOrWhiteSpace(request.Driver)) return "Driver is required.";
    if (string.IsNullOrWhiteSpace(request.Date)) return "Incident date is required.";
    if (string.IsNullOrWhiteSpace(request.Type)) return "Incident type is required.";
    if (string.IsNullOrWhiteSpace(request.Severity)) return "Severity is required.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
    if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000) return "Notes must be 1000 characters or fewer.";
    return null;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
