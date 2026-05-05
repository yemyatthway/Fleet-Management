namespace FleetManagement.Api.Models;

public sealed class FleetDocument
{
  public int Id { get; set; }
  public string OwnerType { get; set; } = string.Empty;
  public string OwnerId { get; set; } = string.Empty;
  public string OwnerName { get; set; } = string.Empty;
  public string DocumentType { get; set; } = string.Empty;
  public string? DocumentNumber { get; set; }
  public string? IssueDate { get; set; }
  public string? ExpiryDate { get; set; }
  public string Status { get; set; } = "Active";
  public string? Notes { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
