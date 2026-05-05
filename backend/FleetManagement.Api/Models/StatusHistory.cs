namespace FleetManagement.Api.Models;

public sealed class StatusHistory
{
  public long Id { get; set; }
  public string EntityType { get; set; } = string.Empty;
  public string EntityId { get; set; } = string.Empty;
  public string? OldStatus { get; set; }
  public string NewStatus { get; set; } = string.Empty;
  public string RoleId { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
}
