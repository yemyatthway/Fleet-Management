namespace FleetManagement.Api.Dtos;

public record AuditLogDto(
  long Id,
  string RoleId,
  string ModuleKey,
  string Action,
  string EntityId,
  string Description,
  DateTime CreatedAt);

public record StatusHistoryDto(
  long Id,
  string EntityType,
  string EntityId,
  string? OldStatus,
  string NewStatus,
  string RoleId,
  DateTime CreatedAt);
