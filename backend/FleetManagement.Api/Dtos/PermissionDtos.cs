namespace FleetManagement.Api.Dtos;

public record PermissionRoleDto(string Id, string Name);

public record RolePermissionDto(
  string RoleId,
  bool CanView,
  bool CanCreate,
  bool CanEdit,
  bool CanDelete);

public record PermissionModuleDto(
  string Key,
  string Name,
  string Category,
  IReadOnlyList<RolePermissionDto> Permissions);

public record PermissionMatrixDto(
  IReadOnlyList<PermissionRoleDto> Roles,
  IReadOnlyList<PermissionModuleDto> Modules);

public record RolePermissionRequest(
  string RoleId,
  string ModuleKey,
  bool CanView,
  bool CanCreate,
  bool CanEdit,
  bool CanDelete);

public record PermissionBulkUpdateRequest(IReadOnlyList<RolePermissionRequest> Permissions);
