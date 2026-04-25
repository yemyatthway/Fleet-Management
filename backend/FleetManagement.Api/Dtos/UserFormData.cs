using Microsoft.AspNetCore.Http;

namespace FleetManagement.Api.Dtos;

public class UserFormData
{
  public string Name { get; set; } = string.Empty;
  public string NrcNumber { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string Role { get; set; } = string.Empty;
  public string? Status { get; set; }
  public string Phone { get; set; } = string.Empty;
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
  public IFormFile? AvatarFile { get; set; }
  public IFormFile? NrcFrontFile { get; set; }
  public IFormFile? NrcBackFile { get; set; }
}
