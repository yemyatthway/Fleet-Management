namespace FleetManagement.Api.Models;

public sealed class Vehicle
{
  public string Id { get; set; } = string.Empty;
  public string Plate { get; set; } = string.Empty;
  public string Region { get; set; } = string.Empty;
  public string Type { get; set; } = string.Empty;
  public string Model { get; set; } = string.Empty;
  public string? Make { get; set; }
  public string? Year { get; set; }
  public string? Color { get; set; }
  public string Status { get; set; } = "Active";
  public string? Ownership { get; set; }
  public string Driver { get; set; } = string.Empty;
  public string? DriverImage { get; set; }
  public string? Depot { get; set; }
  public string? Capacity { get; set; }
  public string? FuelCapacity { get; set; }
  public string FuelType { get; set; } = string.Empty;
  public string? Vin { get; set; }
  public string? EngineNo { get; set; }
  public string? Odometer { get; set; }
  public string? LastService { get; set; }
  public string? NextService { get; set; }
  public string? ServiceNote { get; set; }
  public string? PurchaseCost { get; set; }
  public string? RegistrationNo { get; set; }
  public string? RegistrationExpiry { get; set; }
  public string? RoadTaxExpiry { get; set; }
  public string? InsuranceExpiry { get; set; }
  public string? InsuranceProvider { get; set; }
  public string? InsurancePolicy { get; set; }
  public string? InspectionDue { get; set; }
  public string? AcquiredDate { get; set; }
  public string? Image { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
