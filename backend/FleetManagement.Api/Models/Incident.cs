namespace FleetManagement.Api.Models;

public sealed class Incident
{
  public string Id { get; set; } = string.Empty;
  public string VehicleId { get; set; } = string.Empty;
  public string Driver { get; set; } = string.Empty;
  public string Date { get; set; } = string.Empty;
  public string Type { get; set; } = string.Empty;
  public string Severity { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public string? Cost { get; set; }
  public string? Notes { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
