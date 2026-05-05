namespace FleetManagement.Api.Models;

public sealed class InventoryPart
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string PartNo { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public int Stock { get; set; }
  public int ReorderPoint { get; set; }
  public string? Supplier { get; set; }
  public string? UnitCost { get; set; }
  public string? Location { get; set; }
  public string? Image { get; set; }
  public int IsDeleted { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
