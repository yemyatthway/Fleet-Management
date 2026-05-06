using FleetManagement.Api.Data;
using FleetManagement.Api.Models;

namespace FleetManagement.Api.Auditing;

public static class AuditLogWriter
{
  public static Task LogAuditAsync(FleetDbContext db, HttpRequest request, string moduleKey, string action, string entityId, string description)
  {
    db.AuditLogs.Add(new AuditLog
    {
      RoleId = GetRequestRoleId(request),
      ModuleKey = moduleKey,
      Action = action,
      EntityId = entityId,
      Description = description,
      CreatedAt = DateTime.UtcNow
    });
    return Task.CompletedTask;
  }

  public static string GetRequestRoleId(HttpRequest request) =>
    request.Headers.TryGetValue("X-Fleet-Role-Id", out var roleId) && !string.IsNullOrWhiteSpace(roleId.ToString())
      ? roleId.ToString()
      : "system";

  public static void AddStatusHistoryIfChanged(FleetDbContext db, HttpRequest request, string entityType, string entityId, string? oldStatus, string newStatus)
  {
    if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase)) return;
    db.StatusHistories.Add(new StatusHistory
    {
      EntityType = entityType,
      EntityId = entityId,
      OldStatus = oldStatus,
      NewStatus = newStatus,
      RoleId = GetRequestRoleId(request),
      CreatedAt = DateTime.UtcNow
    });
  }
}
