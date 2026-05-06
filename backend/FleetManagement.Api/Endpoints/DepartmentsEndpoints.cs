using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class DepartmentsEndpoints
{
  public static IEndpointRouteBuilder MapDepartmentsEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/departments", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.DepartmentCodeOptions.AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(department =>
          department.Name.ToLower().Contains(normalizedSearch) ||
          (department.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          department.Status.ToLower().Contains(normalizedSearch));
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("name", "desc") => query.OrderByDescending(department => department.Name),
        ("name", _) => query.OrderBy(department => department.Name),
        ("description", "desc") => query.OrderByDescending(department => department.Description),
        ("description", _) => query.OrderBy(department => department.Description),
        ("status", "desc") => query.OrderByDescending(department => department.Status),
        ("status", _) => query.OrderBy(department => department.Status),
        ("createdat", "desc") => query.OrderByDescending(department => department.CreatedAt),
        ("createdat", _) => query.OrderBy(department => department.CreatedAt),
        ("id", "desc") => query.OrderByDescending(department => department.Id),
        ("id", _) => query.OrderBy(department => department.Id),
        _ => query.OrderBy(department => department.Id)
      };

      var total = await query.CountAsync();
      var items = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Select(department => new DepartmentDto(
          department.Id,
          department.Name,
          department.Description,
          department.Status,
          department.CreatedAt,
          department.UpdatedAt))
        .ToListAsync();

      return Results.Ok(new PagedResult<DepartmentDto>(items, total));
    });

    app.MapGet("/api/departments/options", async (FleetDbContext db) =>
    {
      var items = await db.DepartmentCodeOptions
        .Where(department => department.Status == "Active")
        .OrderBy(department => department.Name)
        .Select(department => department.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    app.MapPost("/api/departments", async (DepartmentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "department-setup", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateDepartmentRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var normalizedName = request.Name.Trim();
      var exists = await db.DepartmentCodeOptions.AnyAsync(department =>
        department.Name.ToLower() == normalizedName.ToLower());

      if (exists) return Results.BadRequest(new ApiError($"{normalizedName} already exists."));

      var now = DateTimeOffset.UtcNow;
      var department = new DepartmentCodeOption
      {
        Name = normalizedName,
        Description = NormalizeOptional(request.Description),
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
        CreatedAt = now,
        UpdatedAt = now
      };

      db.DepartmentCodeOptions.Add(department);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "department-setup", "Create", normalizedName, $"Created department {normalizedName}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToDepartmentDto(department));
    });

    app.MapPut("/api/departments/{id:int}", async (int id, DepartmentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "department-setup", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var validationError = ValidateDepartmentRequest(request);
      if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

      var department = await db.DepartmentCodeOptions.FirstOrDefaultAsync(item => item.Id == id);
      if (department is null) return Results.NotFound(new ApiError("Department not found."));

      var normalizedName = request.Name.Trim();
      var duplicate = await db.DepartmentCodeOptions.AnyAsync(item =>
        item.Id != id && item.Name.ToLower() == normalizedName.ToLower());

      if (duplicate) return Results.BadRequest(new ApiError($"{normalizedName} already exists."));

      department.Name = normalizedName;
      department.Description = NormalizeOptional(request.Description);
      department.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
      department.UpdatedAt = DateTimeOffset.UtcNow;

      await AuditLogWriter.LogAuditAsync(db, httpRequest, "department-setup", "Edit", id.ToString(), $"Updated department {normalizedName}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToDepartmentDto(department));
    });

    app.MapDelete("/api/departments/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "department-setup", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var department = await db.DepartmentCodeOptions.FirstOrDefaultAsync(item => item.Id == id);
      if (department is null) return Results.NotFound(new ApiError("Department not found."));

      var assignedUsers = await db.Users.CountAsync(user => user.IsDeleted == 0 && user.Department == department.Name);
      if (assignedUsers > 0)
      {
        return Results.BadRequest(new ApiError($"Cannot delete {department.Name} while users are assigned to it."));
      }

      db.DepartmentCodeOptions.Remove(department);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "department-setup", "Delete", id.ToString(), $"Deleted department {department.Name}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    return app;
  }

  private static DepartmentDto ToDepartmentDto(DepartmentCodeOption department) =>
    new(
      department.Id,
      department.Name,
      department.Description,
      department.Status,
      department.CreatedAt,
      department.UpdatedAt);

  private static string? ValidateDepartmentRequest(DepartmentRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name)) return "Department name is required.";
    if (request.Name.Trim().Length > 120) return "Department name must be 120 characters or fewer.";
    if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
    {
      return "Department description must be 500 characters or fewer.";
    }

    var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
    return normalizedStatus is "Active" or "Disabled"
      ? null
      : "Department status must be Active or Disabled.";
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
