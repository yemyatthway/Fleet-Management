using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class MaintenanceEndpoints
{
  public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/maintenance-tickets", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? status = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.MaintenanceTickets
        .Where(ticket => ticket.IsDeleted == 0)
        .AsNoTracking()
        .AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(ticket =>
          ticket.Id.ToLower().Contains(normalizedSearch) ||
          ticket.Vehicle.ToLower().Contains(normalizedSearch) ||
          ticket.VehicleId.ToLower().Contains(normalizedSearch) ||
          ticket.Issue.ToLower().Contains(normalizedSearch) ||
          ticket.Mechanic.ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(status))
      {
        var normalizedStatus = status.Trim().ToLower();
        query = query.Where(ticket => ticket.Status.ToLower() == normalizedStatus);
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("id", "asc") => query.OrderBy(ticket => ticket.Id),
        ("id", "desc") => query.OrderByDescending(ticket => ticket.Id),
        ("vehicle", "asc") => query.OrderBy(ticket => ticket.Vehicle),
        ("vehicle", "desc") => query.OrderByDescending(ticket => ticket.Vehicle),
        ("issue", "asc") => query.OrderBy(ticket => ticket.Issue),
        ("issue", "desc") => query.OrderByDescending(ticket => ticket.Issue),
        ("reporteddate", "asc") => query.OrderBy(ticket => ticket.ReportedDate),
        ("reporteddate", "desc") => query.OrderByDescending(ticket => ticket.ReportedDate),
        ("mechanic", "asc") => query.OrderBy(ticket => ticket.Mechanic),
        ("mechanic", "desc") => query.OrderByDescending(ticket => ticket.Mechanic),
        ("status", "asc") => query.OrderBy(ticket => ticket.Status),
        ("status", "desc") => query.OrderByDescending(ticket => ticket.Status),
        _ => query.OrderBy(ticket => ticket.Id)
      };

      var total = await query.CountAsync();
      var statsSource = db.MaintenanceTickets.Where(ticket => ticket.IsDeleted == 0);
      var stats = new MaintenanceTicketStatsDto(
        await statsSource.CountAsync(),
        await statsSource.CountAsync(ticket => ticket.Status == "Pending"),
        await statsSource.CountAsync(ticket => ticket.Status == "Repairing"),
        await statsSource.CountAsync(ticket => ticket.Status == "Completed"));

      var records = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      var items = records
        .Select(ToMaintenanceTicketDto)
        .ToList();

      return Results.Ok(new MaintenanceTicketPagedResult(items, total, stats));
    });

    app.MapPost("/api/maintenance-tickets", async (MaintenanceTicketRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateMaintenanceTicketRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var ticket = new MaintenanceTicket
      {
        Id = NextMaintenanceTicketId(await db.MaintenanceTickets.Select(item => item.Id).ToListAsync()),
        Vehicle = request.Vehicle.Trim(),
        VehicleId = request.VehicleId.Trim(),
        Issue = request.Issue.Trim(),
        Details = request.Details.Trim(),
        ReportedDate = request.ReportedDate.Trim(),
        Mechanic = request.Mechanic.Trim(),
        Status = request.Status.Trim(),
        IsDeleted = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      db.MaintenanceTickets.Add(ticket);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "maintenance-tickets", "Create", ticket.Id, $"Created maintenance ticket {ticket.Id}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToMaintenanceTicketDto(ticket));
    });

    app.MapPut("/api/maintenance-tickets/{ticketId}", async (string ticketId, MaintenanceTicketRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateMaintenanceTicketRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
      if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

      ticket.Vehicle = request.Vehicle.Trim();
      ticket.VehicleId = request.VehicleId.Trim();
      ticket.Issue = request.Issue.Trim();
      ticket.Details = request.Details.Trim();
      ticket.ReportedDate = request.ReportedDate.Trim();
      ticket.Mechanic = request.Mechanic.Trim();
      ticket.Status = request.Status.Trim();
      ticket.UpdatedAt = DateTime.UtcNow;

      await AuditLogWriter.LogAuditAsync(db, httpRequest, "maintenance-tickets", "Edit", ticket.Id, $"Updated maintenance ticket {ticket.Id}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToMaintenanceTicketDto(ticket));
    });

    app.MapPatch("/api/maintenance-tickets/{ticketId}/status", async (string ticketId, MaintenanceTicketStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
      if (string.IsNullOrWhiteSpace(normalizedStatus))
      {
        return Results.BadRequest(new ApiError("Ticket status is required."));
      }

      var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
      if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

      var oldStatus = ticket.Status;
      ticket.Status = normalizedStatus;
      ticket.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "MaintenanceTicket", ticket.Id, oldStatus, ticket.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "maintenance-tickets", "Edit", ticket.Id, $"Changed maintenance ticket status {ticket.Id}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToMaintenanceTicketDto(ticket));
    });

    app.MapDelete("/api/maintenance-tickets/{ticketId}", async (string ticketId, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
      if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

      ticket.IsDeleted = 1;
      ticket.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "maintenance-tickets", "Delete", ticket.Id, $"Deleted maintenance ticket {ticket.Id}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static MaintenanceTicketDto ToMaintenanceTicketDto(MaintenanceTicket ticket) =>
    new(
      ticket.Id,
      ticket.Vehicle,
      ticket.VehicleId,
      ticket.Issue,
      ticket.Details,
      ticket.ReportedDate,
      ticket.Mechanic,
      ticket.Status,
      ticket.CreatedAt,
      ticket.UpdatedAt);

  private static string NextMaintenanceTicketId(IEnumerable<string> existingIds)
  {
    var max = existingIds
      .Select(value =>
      {
        var normalized = value.StartsWith("MT-", StringComparison.OrdinalIgnoreCase)
          ? value[3..]
          : value;
        return int.TryParse(normalized, out var number) ? number : 0;
      })
      .DefaultIfEmpty(2030)
      .Max();

    return $"MT-{max + 1}";
  }

  private static string? ValidateMaintenanceTicketRequest(MaintenanceTicketRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Vehicle)) return "Vehicle is required.";
    if (string.IsNullOrWhiteSpace(request.VehicleId)) return "Vehicle ID is required.";
    if (string.IsNullOrWhiteSpace(request.Issue)) return "Issue is required.";
    if (string.IsNullOrWhiteSpace(request.Details)) return "Details are required.";
    if (string.IsNullOrWhiteSpace(request.ReportedDate)) return "Reported date is required.";
    if (string.IsNullOrWhiteSpace(request.Mechanic)) return "Mechanic is required.";

    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
    return string.IsNullOrWhiteSpace(normalizedStatus) ? "Ticket status is required." : null;
  }
}
