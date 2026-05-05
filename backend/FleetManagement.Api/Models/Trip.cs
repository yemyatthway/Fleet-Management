namespace FleetManagement.Api.Models;

public sealed class Trip
{
  public int Id { get; set; }
  public string TripNumber { get; set; } = string.Empty;
  public string TripType { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public string Priority { get; set; } = string.Empty;
  public string CustomerName { get; set; } = string.Empty;
  public string Department { get; set; } = string.Empty;
  public string? CostCenter { get; set; }
  public string VehicleId { get; set; } = string.Empty;
  public string VehiclePlate { get; set; } = string.Empty;
  public string? TrailerNumber { get; set; }
  public string DriverName { get; set; } = string.Empty;
  public string? CoDriverName { get; set; }
  public string DispatcherName { get; set; } = string.Empty;
  public string CargoType { get; set; } = string.Empty;
  public decimal LoadWeightKg { get; set; }
  public decimal LoadVolumeM3 { get; set; }
  public string PickupLocation { get; set; } = string.Empty;
  public string DropoffLocation { get; set; } = string.Empty;
  public string? PickupContact { get; set; }
  public string? DropoffContact { get; set; }
  public string DepartureDateTime { get; set; } = string.Empty;
  public string EstimatedArrival { get; set; } = string.Empty;
  public string? ActualArrival { get; set; }
  public decimal PlannedDistanceKm { get; set; }
  public decimal StartingOdometerKm { get; set; }
  public decimal CurrentOdometerKm { get; set; }
  public decimal? EndingOdometerKm { get; set; }
  public decimal FuelIssuedLiters { get; set; }
  public decimal TollEstimate { get; set; }
  public bool PermitRequired { get; set; }
  public bool TemperatureControlled { get; set; }
  public string? TemperatureRange { get; set; }
  public string? SpecialInstructions { get; set; }
  public string? DriverNotes { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
