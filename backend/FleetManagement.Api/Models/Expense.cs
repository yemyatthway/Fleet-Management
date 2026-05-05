namespace FleetManagement.Api.Models;

public sealed class Expense
{
  public int Id { get; set; }
  public string ExpenseDate { get; set; } = string.Empty;
  public string ExpenseType { get; set; } = string.Empty;
  public string? VehicleId { get; set; }
  public string? TripNumber { get; set; }
  public string? DriverName { get; set; }
  public decimal Amount { get; set; }
  public string Status { get; set; } = "Active";
  public string? Notes { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
