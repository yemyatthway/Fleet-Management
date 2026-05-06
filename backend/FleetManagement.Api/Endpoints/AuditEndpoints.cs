using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class AuditEndpoints
{
  public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/audit-logs", async (HttpRequest httpRequest, FleetDbContext db, string? module = null, int page = 1, int pageSize = 20) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "audit-logs", PermissionAction.View);
      if (permissionError is not null) return permissionError;

      var query = db.AuditLogs.AsNoTracking().AsQueryable();
      if (!string.IsNullOrWhiteSpace(module) && module != "All") query = query.Where(log => log.ModuleKey == module);
      var total = await query.CountAsync();
      var records = await query
        .OrderByDescending(log => log.CreatedAt)
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      return Results.Ok(new PagedResult<AuditLogDto>(records.Select(ToAuditLogDto).ToList(), total));
    });

    app.MapGet("/api/status-history", async (HttpRequest httpRequest, FleetDbContext db, string? entityType = null, string? entityId = null, int page = 1, int pageSize = 20) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "audit-logs", PermissionAction.View);
      if (permissionError is not null) return permissionError;

      var query = db.StatusHistories.AsNoTracking().AsQueryable();
      if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(history => history.EntityType == entityType);
      if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(history => history.EntityId == entityId);
      var total = await query.CountAsync();
      var records = await query
        .OrderByDescending(history => history.CreatedAt)
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      return Results.Ok(new PagedResult<StatusHistoryDto>(records.Select(ToStatusHistoryDto).ToList(), total));
    });

    return app;
  }

  private static AuditLogDto ToAuditLogDto(AuditLog log) =>
    new(log.Id, log.RoleId, log.ModuleKey, log.Action, log.EntityId, log.Description, log.CreatedAt);

  private static StatusHistoryDto ToStatusHistoryDto(StatusHistory history) =>
    new(history.Id, history.EntityType, history.EntityId, history.OldStatus, history.NewStatus, history.RoleId, history.CreatedAt);
}
