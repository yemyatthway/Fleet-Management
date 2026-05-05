namespace FleetManagement.Api.Models;

public sealed class AuditLog
{
  public long Id { get; set; }
  public string RoleId { get; set; } = string.Empty;
  public string ModuleKey { get; set; } = string.Empty;
  public string Action { get; set; } = string.Empty;
  public string EntityId { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
}
