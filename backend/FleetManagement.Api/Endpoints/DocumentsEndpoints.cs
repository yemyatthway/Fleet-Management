using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class DocumentsEndpoints
{
  public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/documents", async (
      HttpRequest httpRequest,
      FleetDbContext db,
      string? ownerType = null,
      string? search = null,
      string? status = null,
      int page = 1,
      int pageSize = 10) =>
    {
      var moduleKey = ownerType == "Driver" ? "driver-documents" : "vehicle-documents";
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.View);
      if (permissionError is not null) return permissionError;

      var query = db.FleetDocuments.Where(document => document.IsDeleted == 0).AsNoTracking().AsQueryable();
      if (!string.IsNullOrWhiteSpace(ownerType)) query = query.Where(document => document.OwnerType == ownerType);
      if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(document => document.Status == status);
      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(document =>
          document.OwnerId.ToLower().Contains(normalizedSearch) ||
          document.OwnerName.ToLower().Contains(normalizedSearch) ||
          document.DocumentType.ToLower().Contains(normalizedSearch) ||
          (document.DocumentNumber ?? string.Empty).ToLower().Contains(normalizedSearch));
      }
      var total = await query.CountAsync();
      var records = await query.OrderBy(document => document.ExpiryDate).Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
      return Results.Ok(new PagedResult<FleetDocumentDto>(records.Select(ToFleetDocumentDto).ToList(), total));
    });

    app.MapPost("/api/documents", async (FleetDocumentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var moduleKey = request.OwnerType == "Driver" ? "driver-documents" : "vehicle-documents";
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Create);
      if (permissionError is not null) return permissionError;
      var validationError = ValidateFleetDocumentRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
      var now = DateTime.UtcNow;
      var document = new FleetDocument
      {
        OwnerType = request.OwnerType.Trim(),
        OwnerId = request.OwnerId.Trim(),
        OwnerName = request.OwnerName.Trim(),
        DocumentType = request.DocumentType.Trim(),
        DocumentNumber = NormalizeOptional(request.DocumentNumber),
        IssueDate = NormalizeOptional(request.IssueDate),
        ExpiryDate = NormalizeOptional(request.ExpiryDate),
        Status = request.Status.Trim(),
        Notes = NormalizeOptional(request.Notes),
        CreatedAt = now,
        UpdatedAt = now
      };
      db.FleetDocuments.Add(document);
      await db.SaveChangesAsync();
      await AuditLogWriter.LogAuditAsync(db, httpRequest, moduleKey, "Create", document.Id.ToString(), $"Created {document.OwnerType} document {document.DocumentType}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToFleetDocumentDto(document));
    });

    app.MapPut("/api/documents/{id:int}", async (int id, FleetDocumentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var moduleKey = request.OwnerType == "Driver" ? "driver-documents" : "vehicle-documents";
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Edit);
      if (permissionError is not null) return permissionError;
      var validationError = ValidateFleetDocumentRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
      var document = await db.FleetDocuments.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (document is null) return Results.NotFound(new ApiError("Document not found."));
      var oldStatus = document.Status;
      document.OwnerType = request.OwnerType.Trim();
      document.OwnerId = request.OwnerId.Trim();
      document.OwnerName = request.OwnerName.Trim();
      document.DocumentType = request.DocumentType.Trim();
      document.DocumentNumber = NormalizeOptional(request.DocumentNumber);
      document.IssueDate = NormalizeOptional(request.IssueDate);
      document.ExpiryDate = NormalizeOptional(request.ExpiryDate);
      document.Status = request.Status.Trim();
      document.Notes = NormalizeOptional(request.Notes);
      document.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "Document", document.Id.ToString(), oldStatus, document.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, moduleKey, "Edit", document.Id.ToString(), $"Updated {document.OwnerType} document {document.DocumentType}.");
      await db.SaveChangesAsync();
      return Results.Ok(ToFleetDocumentDto(document));
    });

    app.MapDelete("/api/documents/{id:int}", async (int id, string ownerType, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var moduleKey = ownerType == "Driver" ? "driver-documents" : "vehicle-documents";
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Delete);
      if (permissionError is not null) return permissionError;
      var document = await db.FleetDocuments.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
      if (document is null) return Results.NotFound(new ApiError("Document not found."));
      document.IsDeleted = 1;
      document.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, moduleKey, "Delete", document.Id.ToString(), $"Deleted {document.OwnerType} document {document.DocumentType}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static FleetDocumentDto ToFleetDocumentDto(FleetDocument document) =>
    new(
      document.Id,
      document.OwnerType,
      document.OwnerId,
      document.OwnerName,
      document.DocumentType,
      document.DocumentNumber,
      document.IssueDate,
      document.ExpiryDate,
      document.Status,
      document.Notes,
      document.CreatedAt,
      document.UpdatedAt);

  private static string? ValidateFleetDocumentRequest(FleetDocumentRequest request)
  {
    if (request.OwnerType is not ("Vehicle" or "Driver")) return "Document owner type must be Vehicle or Driver.";
    if (string.IsNullOrWhiteSpace(request.OwnerId)) return "Owner ID is required.";
    if (string.IsNullOrWhiteSpace(request.OwnerName)) return "Owner name is required.";
    if (string.IsNullOrWhiteSpace(request.DocumentType)) return "Document type is required.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
    if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000) return "Notes must be 1000 characters or fewer.";
    return null;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
