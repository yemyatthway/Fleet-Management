namespace FleetManagement.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record AuthUserDto(
  string Id,
  string Name,
  string Email,
  string RoleId,
  string Role,
  string Status,
  string Avatar);

public record UserPermissionDto(
  string ModuleKey,
  bool CanView,
  bool CanCreate,
  bool CanEdit,
  bool CanDelete);

public record LoginResponseDto(
  AuthUserDto User,
  IReadOnlyList<UserPermissionDto> Permissions);
