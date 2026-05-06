using FleetManagement.Api.Assets;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class RolesEndpoints
{
  public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/roles", async (
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? role = null,
      string? status = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var fixedRoleIds = SeedData.FixedRoleIds;
      var query = db.Roles.Where(r => r.IsDeleted == 0 && fixedRoleIds.Contains(r.Id)).AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(r =>
          r.Name.ToLower().Contains(normalizedSearch) ||
          (r.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
          r.Status.ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(role))
      {
        var normalizedRole = role.Trim().ToLower();
        query = query.Where(r => r.Name.ToLower() == normalizedRole);
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("code", "desc") => query.OrderByDescending(r => r.Code),
        ("code", _) => query.OrderBy(r => r.Code),
        ("name", "desc") => query.OrderByDescending(r => r.Name),
        ("name", _) => query.OrderBy(r => r.Name),
        ("description", "desc") => query.OrderByDescending(r => r.Description),
        ("description", _) => query.OrderBy(r => r.Description),
        ("status", "desc") => query.OrderByDescending(r => r.Status),
        ("status", _) => query.OrderBy(r => r.Status),
        ("createdat", "desc") => query.OrderByDescending(r => r.CreatedAt),
        ("createdat", _) => query.OrderBy(r => r.CreatedAt),
        ("members", "desc") => query.OrderByDescending(r => r.Users.Count(u => u.IsDeleted == 0)),
        ("members", _) => query.OrderBy(r => r.Users.Count(u => u.IsDeleted == 0)),
        ("id", "desc") => query.OrderByDescending(r => r.Code),
        ("id", _) => query.OrderBy(r => r.Code),
        _ => query.OrderBy(r => r.Code)
      };

      var total = await query.CountAsync();
      var items = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Select(r => new RoleDto(
          r.Id,
          r.Code,
          r.Name,
          r.Description,
          r.Status,
          r.Users.Count(u => u.IsDeleted == 0),
          r.CreatedAt,
          r.UpdatedAt))
        .ToListAsync();

      return Results.Ok(new PagedResult<RoleDto>(items, total));
    });

    app.MapGet("/api/roles/options", async (FleetDbContext db) =>
    {
      var fixedRoleIds = SeedData.FixedRoleIds;
      var items = await db.Roles
        .Where(r => r.IsDeleted == 0 && fixedRoleIds.Contains(r.Id))
        .OrderBy(r => r.Name)
        .Select(r => r.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    app.MapGet("/api/roles/{roleId}/members", async (string roleId, HttpRequest request, FleetDbContext db) =>
    {
      var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId && r.IsDeleted == 0);
      if (!roleExists) return Results.NotFound(new ApiError("Role not found."));

      var members = await db.Users
        .Where(u => u.RoleId == roleId && u.IsDeleted == 0)
        .OrderBy(u => u.Name)
        .ToListAsync();

      var items = members
        .Select(u => new RoleMemberDto(
          u.Id,
          u.Name,
          u.Email,
          u.Phone,
          u.Status,
          u.JoinDate,
          PublicAssetUrls.ToPublicAssetUrl(request, u.Avatar)))
        .ToList();

      return Results.Ok(items);
    });

    app.MapPost("/api/roles", async (RoleRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "roles", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be created."));
    });

    app.MapPut("/api/roles/{roleId}", async (string roleId, RoleRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "roles", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be edited."));
    });

    app.MapDelete("/api/roles/{roleId}", async (string roleId, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "roles", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be deleted."));
    });

    return app;
  }
}
