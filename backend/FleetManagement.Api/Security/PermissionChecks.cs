using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Security;

public static class PermissionChecks
{
  public static async Task<IResult?> RequirePermissionAsync(
    HttpRequest request,
    FleetDbContext db,
    string moduleKey,
    PermissionAction action)
  {
    var roleId = request.Headers["X-Fleet-Role-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(roleId))
    {
      return Results.Json(new ApiError("Login session is required."), statusCode: StatusCodes.Status401Unauthorized);
    }

    var savedPermission = await db.RolePermissions
      .AsNoTracking()
      .FirstOrDefaultAsync(permission =>
        permission.RoleId == roleId &&
        permission.ModuleKey == moduleKey);

    var defaultPermission = GetDefaultPermission(roleId, moduleKey);
    var allowed = action switch
    {
      PermissionAction.View => savedPermission?.CanView ?? defaultPermission.CanView,
      PermissionAction.Create => savedPermission?.CanCreate ?? defaultPermission.CanCreate,
      PermissionAction.Edit => savedPermission?.CanEdit ?? defaultPermission.CanEdit,
      PermissionAction.Delete => savedPermission?.CanDelete ?? defaultPermission.CanDelete,
      _ => false
    };

    return allowed
      ? null
      : Results.Json(new ApiError("You do not have permission to perform this action."), statusCode: StatusCodes.Status403Forbidden);
  }

  public static RolePermissionDto GetDefaultPermission(string roleId, string moduleKey)
  {
    if (roleId.Equals("admin", StringComparison.OrdinalIgnoreCase))
    {
      return new RolePermissionDto(roleId, true, true, true, true);
    }

    var viewOnly = new RolePermissionDto(roleId, true, false, false, false);
    var none = new RolePermissionDto(roleId, false, false, false, false);

    return roleId.ToLowerInvariant() switch
    {
      "dispatcher" when moduleKey is "dashboard" or "vehicles" or "trips" or "reports" or "expenses" or "location-setup" => viewOnly with { CanCreate = moduleKey is "trips" or "expenses", CanEdit = moduleKey is "trips" or "expenses" },
      "driver" when moduleKey is "dashboard" or "trips" or "vehicles" => viewOnly,
      "mechanic" when moduleKey is "dashboard" or "vehicles" or "maintenance-tickets" or "inventory-parts" or "incidents" => viewOnly with { CanCreate = moduleKey is "maintenance-tickets" or "incidents", CanEdit = moduleKey is "maintenance-tickets" or "inventory-parts" or "incidents" },
      _ => none
    };
  }

  public static async Task<IReadOnlyList<UserPermissionDto>> GetPermissionsForRoleAsync(FleetDbContext db, string roleId)
  {
    var modules = PermissionModules.All;
    var moduleKeys = modules.Select(module => module.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var savedPermissions = await db.RolePermissions
      .AsNoTracking()
      .Where(permission => permission.RoleId == roleId && moduleKeys.Contains(permission.ModuleKey))
      .ToListAsync();
    var savedLookup = savedPermissions.ToDictionary(permission => permission.ModuleKey, StringComparer.OrdinalIgnoreCase);

    return modules.Select(module =>
    {
      var hasSaved = savedLookup.TryGetValue(module.Key, out var saved);
      var defaultPermission = GetDefaultPermission(roleId, module.Key);
      return new UserPermissionDto(
        module.Key,
        saved?.CanView ?? defaultPermission.CanView,
        saved?.CanCreate ?? defaultPermission.CanCreate,
        saved?.CanEdit ?? defaultPermission.CanEdit,
        saved?.CanDelete ?? defaultPermission.CanDelete);
    }).ToList();
  }
}
