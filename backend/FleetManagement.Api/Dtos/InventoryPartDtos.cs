namespace FleetManagement.Api.Dtos;

public record InventoryPartDto(
  string Id,
  string Name,
  string PartNo,
  string Category,
  int Stock,
  int ReorderPoint,
  string? Supplier,
  string? UnitCost,
  string? Location,
  string? Image,
  DateTime CreatedAt,
  DateTime UpdatedAt);

public sealed class InventoryPartForm
{
  public string Name { get; set; } = string.Empty;
  public string PartNo { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public int Stock { get; set; }
  public int ReorderPoint { get; set; }
  public string? Supplier { get; set; }
  public string? UnitCost { get; set; }
  public string? Location { get; set; }
  public bool RemoveImage { get; set; }
  public IFormFile? ImageFile { get; set; }
}
