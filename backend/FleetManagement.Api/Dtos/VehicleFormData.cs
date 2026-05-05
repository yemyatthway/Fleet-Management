using Microsoft.AspNetCore.Http;

namespace FleetManagement.Api.Dtos;

public class VehicleFormData
{
  public string? Plate { get; set; }
  public string? Region { get; set; }
  public string? Type { get; set; }
  public string? Model { get; set; }
  public string? Make { get; set; }
  public string? Year { get; set; }
  public string? Color { get; set; }
  public string? Status { get; set; }
  public string? Ownership { get; set; }
  public string? Driver { get; set; }
  public string? Depot { get; set; }
  public string? Capacity { get; set; }
  public string? FuelCapacity { get; set; }
  public string? FuelType { get; set; }
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
  public bool RemoveVehicleImage { get; set; }
  public bool RemoveDriverImage { get; set; }
  public IFormFile? VehicleImageFile { get; set; }
  public IFormFile? DriverImageFile { get; set; }
}
