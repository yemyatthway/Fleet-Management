using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class PermissionsEndpoints
{
  public static IEndpointRouteBuilder MapPermissionsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/permissions", async (FleetDbContext db) =>
    {
      return Results.Ok(await PermissionMatrixBuilder.BuildPermissionMatrixAsync(db));
    });

    app.MapPut("/api/permissions", async (PermissionBulkUpdateRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "permissions", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var fixedRoleIds = SeedData.FixedRoleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
      var moduleKeys = PermissionModules.All.Select(module => module.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
      var submitted = request.Permissions ?? [];

      foreach (var permission in submitted)
      {
        if (!fixedRoleIds.Contains(permission.RoleId))
        {
          return Results.BadRequest(new ApiError("Permission role is not a fixed system role."));
        }

        if (!moduleKeys.Contains(permission.ModuleKey))
        {
          return Results.BadRequest(new ApiError("Permission module does not exist."));
        }
      }

      var now = DateTime.UtcNow;
      foreach (var permission in submitted)
      {
        var normalizedRoleId = permission.RoleId.Trim();
        var normalizedModuleKey = permission.ModuleKey.Trim();
        var existingPermission = await db.RolePermissions.FirstOrDefaultAsync(item =>
          item.RoleId == normalizedRoleId && item.ModuleKey == normalizedModuleKey);

        if (existingPermission is null)
        {
          db.RolePermissions.Add(new RolePermission
          {
            RoleId = normalizedRoleId,
            ModuleKey = normalizedModuleKey,
            CanView = permission.CanView,
            CanCreate = permission.CanCreate,
            CanEdit = permission.CanEdit,
            CanDelete = permission.CanDelete,
            CreatedAt = now,
            UpdatedAt = now
          });
          continue;
        }

        existingPermission.CanView = permission.CanView;
        existingPermission.CanCreate = permission.CanCreate;
        existingPermission.CanEdit = permission.CanEdit;
        existingPermission.CanDelete = permission.CanDelete;
        existingPermission.UpdatedAt = now;
      }

      await db.SaveChangesAsync();
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "permissions", "Edit", "matrix", "Updated role permission matrix.");
      await db.SaveChangesAsync();
      return Results.Ok(await PermissionMatrixBuilder.BuildPermissionMatrixAsync(db));
    });

    return app;
  }
}
