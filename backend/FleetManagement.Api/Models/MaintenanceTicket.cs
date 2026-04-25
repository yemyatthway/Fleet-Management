namespace FleetManagement.Api.Models;

public sealed class MaintenanceTicket
{
  public string Id { get; set; } = string.Empty;
  public string Vehicle { get; set; } = string.Empty;
  public string VehicleId { get; set; } = string.Empty;
  public string Issue { get; set; } = string.Empty;
  public string Details { get; set; } = string.Empty;
  public string ReportedDate { get; set; } = string.Empty;
  public string Mechanic { get; set; } = string.Empty;
  public string Status { get; set; } = "Pending";
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
