namespace FleetManagement.Api.Models;

public class RoleMember
{
  public string Id { get; set; } = string.Empty;
  public string RoleId { get; set; } = string.Empty;
  public string RoleName { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string Phone { get; set; } = string.Empty;
  public string Status { get; set; } = "Active";
  public string JoinDate { get; set; } = string.Empty;
  public string Avatar { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public Role? Role { get; set; }
}
