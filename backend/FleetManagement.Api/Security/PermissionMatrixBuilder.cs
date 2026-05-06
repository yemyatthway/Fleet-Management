using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Security;

public static class PermissionMatrixBuilder
{
  public static async Task<PermissionMatrixDto> BuildPermissionMatrixAsync(FleetDbContext db)
  {
    var fixedRoleIds = SeedData.FixedRoleIds;
    var roles = await db.Roles
      .AsNoTracking()
      .Where(role => role.IsDeleted == 0 && fixedRoleIds.Contains(role.Id))
      .OrderBy(role => role.Code)
      .Select(role => new PermissionRoleDto(role.Id, role.Name))
      .ToListAsync();

    var modules = PermissionModules.All;
    var moduleKeys = modules.Select(module => module.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var permissions = await db.RolePermissions
      .AsNoTracking()
      .Where(permission => fixedRoleIds.Contains(permission.RoleId) && moduleKeys.Contains(permission.ModuleKey))
      .ToListAsync();

    var permissionLookup = permissions.ToDictionary(
      permission => $"{permission.RoleId}:{permission.ModuleKey}",
      StringComparer.OrdinalIgnoreCase);

    var moduleDtos = modules
      .Select(module => new PermissionModuleDto(
        module.Key,
        module.Name,
        module.Category,
        roles.Select(role =>
        {
          var hasPermission = permissionLookup.TryGetValue($"{role.Id}:{module.Key}", out var permission);
          var defaultPermission = PermissionChecks.GetDefaultPermission(role.Id, module.Key);
          return new RolePermissionDto(
            role.Id,
            permission?.CanView ?? defaultPermission.CanView,
            permission?.CanCreate ?? defaultPermission.CanCreate,
            permission?.CanEdit ?? defaultPermission.CanEdit,
            permission?.CanDelete ?? defaultPermission.CanDelete);
        }).ToList()))
      .ToList();

    return new PermissionMatrixDto(roles, moduleDtos);
  }
}
