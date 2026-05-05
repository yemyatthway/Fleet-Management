namespace FleetManagement.Api.Models;

public sealed class RolePermission
{
  public int Id { get; set; }
  public string RoleId { get; set; } = string.Empty;
  public string ModuleKey { get; set; } = string.Empty;
  public bool CanView { get; set; }
  public bool CanCreate { get; set; }
  public bool CanEdit { get; set; }
  public bool CanDelete { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Role? Role { get; set; }
}
