namespace FleetManagement.Api.Models;

public abstract class TripSetupCodeOption
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Code { get; set; } = string.Empty;
  public string? Description { get; set; }
  public string Status { get; set; } = "Active";
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TripTypeCodeOption : TripSetupCodeOption;
public sealed class CargoTypeCodeOption : TripSetupCodeOption;
public sealed class StatusCodeOption : TripSetupCodeOption;
public sealed class TripPriorityCodeOption : TripSetupCodeOption;
public sealed class IncidentTypeCodeOption : TripSetupCodeOption;
public sealed class SeverityCodeOption : TripSetupCodeOption;
public sealed class ExpenseTypeCodeOption : TripSetupCodeOption;
public sealed class MaintenanceTypeCodeOption : TripSetupCodeOption;
public sealed class DocumentTypeCodeOption : TripSetupCodeOption;
