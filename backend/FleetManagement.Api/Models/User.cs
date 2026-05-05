namespace FleetManagement.Api.Models;

public class User
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string EmployeeId { get; set; } = string.Empty;
  public string NrcNumber { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string PasswordHash { get; set; } = string.Empty;
  public string RoleId { get; set; } = string.Empty;
  public string Status { get; set; } = "Active";
  public string Phone { get; set; } = string.Empty;
  public string Avatar { get; set; } = string.Empty;
  public string NrcFront { get; set; } = string.Empty;
  public string NrcBack { get; set; } = string.Empty;
  public string Department { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public string Location { get; set; } = string.Empty;
  public string Manager { get; set; } = string.Empty;
  public string? LicenseNumber { get; set; }
  public string? LicenseClass { get; set; }
  public string? LicenseExpiry { get; set; }
  public string EmergencyContactName { get; set; } = string.Empty;
  public string EmergencyContactRelation { get; set; } = string.Empty;
  public string EmergencyContactPhone { get; set; } = string.Empty;
  public string Address { get; set; } = string.Empty;
  public bool TwoFactorEnabled { get; set; }
  public string? Notes { get; set; }
  public string JoinDate { get; set; } = string.Empty;
  public string? LastLogin { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public Role? Role { get; set; }
}
