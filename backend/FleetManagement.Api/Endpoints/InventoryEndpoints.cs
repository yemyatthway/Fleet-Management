using FleetManagement.Api.Assets;
using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class InventoryEndpoints
{
  public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/inventory-parts", async (
      HttpRequest httpRequest,
      FleetDbContext db,
      string? search = null,
      string? category = null,
      string? stock = null) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.View);
      if (permissionError is not null) return permissionError;

      var query = db.InventoryParts
        .Where(part => part.IsDeleted == 0)
        .AsNoTracking()
        .AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(part =>
          part.Name.ToLower().Contains(normalizedSearch) ||
          part.PartNo.ToLower().Contains(normalizedSearch) ||
          part.Category.ToLower().Contains(normalizedSearch) ||
          (part.Supplier ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          (part.Location ?? string.Empty).ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(category) && category != "All")
      {
        query = query.Where(part => part.Category == category);
      }

      if (stock == "Low")
      {
        query = query.Where(part => part.Stock <= part.ReorderPoint);
      }
      else if (stock == "Healthy")
      {
        query = query.Where(part => part.Stock > part.ReorderPoint);
      }

      var items = await query
        .OrderBy(part => part.Name)
        .ToListAsync();

      return Results.Ok(items.Select(part => ToInventoryPartDto(part, httpRequest)).ToList());
    });

    app.MapPost("/api/inventory-parts", async ([FromForm] InventoryPartForm form, HttpRequest httpRequest, FleetDbContext db, IWebHostEnvironment environment) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateInventoryPartRequest(form);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var duplicate = await db.InventoryParts.AnyAsync(part =>
        part.IsDeleted == 0 && part.PartNo.ToLower() == form.PartNo.Trim().ToLower());
      if (duplicate) return Results.BadRequest(new ApiError("Part number already exists."));

      var now = DateTime.UtcNow;
      var partId = NextInventoryPartId(await db.InventoryParts.Select(item => item.Id).ToListAsync());
      var part = new InventoryPart
      {
        Id = partId,
        Name = form.Name.Trim(),
        PartNo = form.PartNo.Trim(),
        Category = form.Category.Trim(),
        Stock = form.Stock,
        ReorderPoint = form.ReorderPoint,
        Supplier = NormalizeOptional(form.Supplier),
        UnitCost = NormalizeOptional(form.UnitCost),
        Location = NormalizeOptional(form.Location),
        IsDeleted = 0,
        CreatedAt = now,
        UpdatedAt = now
      };
      if (form.ImageFile is not null)
      {
        part.Image = await UserAssetStorage.SaveImageAsync(form.ImageFile, "inventory-parts", partId, "part-image", environment);
      }

      db.InventoryParts.Add(part);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "inventory-parts", "Create", part.Id, $"Created inventory part {part.Name}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToInventoryPartDto(part, httpRequest));
    }).DisableAntiforgery();

    app.MapPut("/api/inventory-parts/{partId}", async (string partId, [FromForm] InventoryPartForm form, HttpRequest httpRequest, FleetDbContext db, IWebHostEnvironment environment) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateInventoryPartRequest(form);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var part = await db.InventoryParts.FirstOrDefaultAsync(item => item.Id == partId && item.IsDeleted == 0);
      if (part is null) return Results.NotFound(new ApiError("Inventory part not found."));

      var duplicate = await db.InventoryParts.AnyAsync(item =>
        item.Id != partId &&
        item.IsDeleted == 0 &&
        item.PartNo.ToLower() == form.PartNo.Trim().ToLower());
      if (duplicate) return Results.BadRequest(new ApiError("Part number already exists."));

      part.Name = form.Name.Trim();
      part.PartNo = form.PartNo.Trim();
      part.Category = form.Category.Trim();
      part.Stock = form.Stock;
      part.ReorderPoint = form.ReorderPoint;
      part.Supplier = NormalizeOptional(form.Supplier);
      part.UnitCost = NormalizeOptional(form.UnitCost);
      part.Location = NormalizeOptional(form.Location);
      if (form.RemoveImage)
      {
        UserAssetStorage.DeleteStoredAsset("inventory-parts", part.Id, part.Image, environment);
        part.Image = null;
      }
      if (form.ImageFile is not null)
      {
        part.Image = await UserAssetStorage.SaveImageAsync(form.ImageFile, "inventory-parts", part.Id, "part-image", environment);
      }
      part.UpdatedAt = DateTime.UtcNow;

      await AuditLogWriter.LogAuditAsync(db, httpRequest, "inventory-parts", "Edit", part.Id, $"Updated inventory part {part.Name}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToInventoryPartDto(part, httpRequest));
    }).DisableAntiforgery();

    app.MapDelete("/api/inventory-parts/{partId}", async (string partId, HttpRequest httpRequest, IWebHostEnvironment environment, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var part = await db.InventoryParts.FirstOrDefaultAsync(item => item.Id == partId && item.IsDeleted == 0);
      if (part is null) return Results.NotFound(new ApiError("Inventory part not found."));

      part.IsDeleted = 1;
      part.Image = null;
      part.UpdatedAt = DateTime.UtcNow;
      UserAssetStorage.DeleteEntityDirectory("inventory-parts", part.Id, environment);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "inventory-parts", "Delete", part.Id, $"Deleted inventory part {part.Name}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static InventoryPartDto ToInventoryPartDto(InventoryPart part, HttpRequest request) =>
    new(
      part.Id,
      part.Name,
      part.PartNo,
      part.Category,
      part.Stock,
      part.ReorderPoint,
      part.Supplier,
      part.UnitCost,
      part.Location,
      PublicAssetUrls.ToPublicAssetUrl(request, part.Image),
      part.CreatedAt,
      part.UpdatedAt);

  private static string NextInventoryPartId(IEnumerable<string> existingIds)
  {
    var max = existingIds
      .Select(value =>
      {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var normalized = value.StartsWith("PRT-", StringComparison.OrdinalIgnoreCase)
          ? value[4..]
          : value;
        return int.TryParse(normalized, out var number) ? number : 0;
      })
      .DefaultIfEmpty(4000)
      .Max();

    return $"PRT-{max + 1}";
  }

  private static string? ValidateInventoryPartRequest(InventoryPartForm request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Part name is required.";
    if (string.IsNullOrWhiteSpace(request.PartNo)) return "Part number is required.";
    if (string.IsNullOrWhiteSpace(request.Category)) return "Category is required.";
    if (request.Stock < 0) return "Stock cannot be negative.";
    if (request.ReorderPoint < 0) return "Reorder point cannot be negative.";
    if (!string.IsNullOrWhiteSpace(request.Supplier) && request.Supplier.Trim().Length > 160) return "Supplier must be 160 characters or fewer.";
    if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Trim().Length > 160) return "Location must be 160 characters or fewer.";
    return null;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
