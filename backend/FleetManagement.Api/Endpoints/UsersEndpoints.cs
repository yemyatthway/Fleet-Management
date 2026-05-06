using FleetManagement.Api.Assets;
using FleetManagement.Api.Auditing;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class UsersEndpoints
{
  public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapGet("/api/users", async (
      HttpRequest request,
      FleetDbContext db,
      int page = 1,
      int pageSize = 10,
      string? search = null,
      string? role = null,
      string? status = null,
      string? sortBy = "id",
      string? sortOrder = "asc") =>
    {
      var query = db.Users
        .Include(u => u.Role)
        .Where(u => u.IsDeleted == 0 && u.Role != null && u.Role.IsDeleted == 0)
        .AsQueryable();

      if (!string.IsNullOrWhiteSpace(search))
      {
        var normalizedSearch = search.Trim().ToLower();
        query = query.Where(u =>
          u.Name.ToLower().Contains(normalizedSearch) ||
          u.Email.ToLower().Contains(normalizedSearch) ||
          u.EmployeeId.ToLower().Contains(normalizedSearch) ||
          u.Department.ToLower().Contains(normalizedSearch) ||
          u.Location.ToLower().Contains(normalizedSearch));
      }

      if (!string.IsNullOrWhiteSpace(role))
      {
        var normalizedRole = role.Trim().ToLower();
        query = query.Where(u => u.Role!.Name.ToLower() == normalizedRole);
      }

      if (!string.IsNullOrWhiteSpace(status))
      {
        var normalizedStatus = status.Trim().ToLower();
        query = query.Where(u => u.Status.ToLower() == normalizedStatus);
      }

      query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
      {
        ("name", "desc") => query.OrderByDescending(u => u.Name),
        ("name", _) => query.OrderBy(u => u.Name),
        ("employeeid", "desc") => query.OrderByDescending(u => u.EmployeeId),
        ("employeeid", _) => query.OrderBy(u => u.EmployeeId),
        ("email", "desc") => query.OrderByDescending(u => u.Email),
        ("email", _) => query.OrderBy(u => u.Email),
        ("role", "desc") => query.OrderByDescending(u => u.Role!.Name),
        ("role", _) => query.OrderBy(u => u.Role!.Name),
        ("status", "desc") => query.OrderByDescending(u => u.Status),
        ("status", _) => query.OrderBy(u => u.Status),
        ("joindate", "desc") => query.OrderByDescending(u => u.JoinDate),
        ("joindate", _) => query.OrderBy(u => u.JoinDate),
        ("lastlogin", "desc") => query.OrderByDescending(u => u.LastLogin),
        ("lastlogin", _) => query.OrderBy(u => u.LastLogin),
        ("department", "desc") => query.OrderByDescending(u => u.Department),
        ("department", _) => query.OrderBy(u => u.Department),
        ("location", "desc") => query.OrderByDescending(u => u.Location),
        ("location", _) => query.OrderBy(u => u.Location),
        ("id", "desc") => query.OrderByDescending(u => u.Id.Length).ThenByDescending(u => u.Id),
        _ => query.OrderBy(u => u.Id.Length).ThenBy(u => u.Id)
      };

      var total = await query.CountAsync();
      var stats = new UserStatsDto(
        total,
        await query.CountAsync(u => u.Status == "Active"),
        await query.CountAsync(u => u.Role!.Name == "Driver"),
        await query.CountAsync(u => u.Role!.Name == "Admin"));

      var users = await query
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .ToListAsync();

      var items = users
        .Select(u => ToUserDto(u, u.Role!.Name, request))
        .ToList();

      return Results.Ok(new UserPagedResult(items, total, stats));
    });

    app.MapPost("/api/users", async (
      [FromForm] UserFormData form,
      HttpRequest request,
      IWebHostEnvironment environment,
      FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(request, db, "users", PermissionAction.Create);
      if (permissionError is not null) return permissionError;

      var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == form.Role && r.IsDeleted == 0);
      if (roleEntity is null) return Results.BadRequest(new ApiError("Selected role does not exist."));

      if (string.IsNullOrWhiteSpace(form.Status)) return Results.BadRequest(new ApiError("User status is required."));

      var normalizedDepartment = form.Department.Trim();
      var departmentExists = await db.DepartmentCodeOptions.AnyAsync(department =>
        department.Status == "Active" && department.Name.ToLower() == normalizedDepartment.ToLower());
      if (!departmentExists) return Results.BadRequest(new ApiError("Selected department does not exist."));

      if (form.AvatarFile is null || form.NrcFrontFile is null || form.NrcBackFile is null)
      {
        return Results.BadRequest(new ApiError("Profile, NRC front, and NRC back images are required."));
      }

      var duplicateEmail = await db.Users.AnyAsync(u => u.IsDeleted == 0 && u.Email.ToLower() == form.Email.Trim().ToLower());
      if (duplicateEmail) return Results.BadRequest(new ApiError("Email already exists."));

      var now = DateTime.UtcNow;
      var existingIds = await db.Users.Select(u => u.Id).ToListAsync();
      var nextId = existingIds
        .Select(id => int.TryParse(id, out var value) ? value : 0)
        .DefaultIfEmpty(0)
        .Max() + 1;

      var existingEmployeeIds = await db.Users.Select(u => u.EmployeeId).ToListAsync();

      var user = new User
      {
        Id = nextId.ToString(),
        Name = form.Name.Trim(),
        EmployeeId = NextEmployeeId(existingEmployeeIds),
        NrcNumber = form.NrcNumber.Trim(),
        Email = form.Email.Trim(),
        RoleId = roleEntity.Id,
        PasswordHash = SeedData.HashPassword("Password@123"),
        Status = form.Status.Trim(),
        Phone = form.Phone.Trim(),
        Avatar = string.Empty,
        NrcFront = string.Empty,
        NrcBack = string.Empty,
        Department = normalizedDepartment,
        Title = form.Title.Trim(),
        Location = form.Location.Trim(),
        Manager = form.Manager.Trim(),
        LicenseNumber = string.IsNullOrWhiteSpace(form.LicenseNumber) ? null : form.LicenseNumber.Trim(),
        LicenseClass = string.IsNullOrWhiteSpace(form.LicenseClass) ? null : form.LicenseClass.Trim(),
        LicenseExpiry = string.IsNullOrWhiteSpace(form.LicenseExpiry) ? null : form.LicenseExpiry.Trim(),
        EmergencyContactName = form.EmergencyContactName.Trim(),
        EmergencyContactRelation = form.EmergencyContactRelation.Trim(),
        EmergencyContactPhone = form.EmergencyContactPhone.Trim(),
        Address = form.Address.Trim(),
        TwoFactorEnabled = form.TwoFactorEnabled,
        Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim(),
        JoinDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        LastLogin = DateTime.UtcNow.ToString("o"),
        IsDeleted = 0,
        CreatedAt = now,
        UpdatedAt = now
      };

      db.Users.Add(user);
      await db.SaveChangesAsync();

      user.Avatar = await UserAssetStorage.SaveImageAsync(form.AvatarFile, user.Id, "avatar", environment);
      user.NrcFront = await UserAssetStorage.SaveImageAsync(form.NrcFrontFile, user.Id, "nrc-front", environment);
      user.NrcBack = await UserAssetStorage.SaveImageAsync(form.NrcBackFile, user.Id, "nrc-back", environment);
      user.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, request, "users", "Create", user.Id, $"Created user {user.Name}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToUserDto(user, roleEntity.Name, request));
    }).DisableAntiforgery();

    app.MapPut("/api/users/{userId}", async (
      string userId,
      [FromForm] UserFormData form,
      HttpRequest request,
      IWebHostEnvironment environment,
      FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(request, db, "users", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
      if (user is null) return Results.NotFound(new ApiError("User not found."));

      var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == form.Role && r.IsDeleted == 0);
      if (roleEntity is null) return Results.BadRequest(new ApiError("Selected role does not exist."));

      if (string.IsNullOrWhiteSpace(form.Status)) return Results.BadRequest(new ApiError("User status is required."));

      var normalizedDepartment = form.Department.Trim();
      var departmentExists = await db.DepartmentCodeOptions.AnyAsync(department =>
        department.Status == "Active" && department.Name.ToLower() == normalizedDepartment.ToLower());
      if (!departmentExists) return Results.BadRequest(new ApiError("Selected department does not exist."));

      var duplicateEmail = await db.Users.AnyAsync(u => u.Id != userId && u.IsDeleted == 0 && u.Email.ToLower() == form.Email.Trim().ToLower());
      if (duplicateEmail) return Results.BadRequest(new ApiError("Email already exists."));

      user.Name = form.Name.Trim();
      user.NrcNumber = form.NrcNumber.Trim();
      user.Email = form.Email.Trim();
      user.RoleId = roleEntity.Id;
      user.Status = form.Status.Trim();
      user.Phone = form.Phone.Trim();
      user.Department = normalizedDepartment;
      user.Title = form.Title.Trim();
      user.Location = form.Location.Trim();
      user.Manager = form.Manager.Trim();
      user.LicenseNumber = string.IsNullOrWhiteSpace(form.LicenseNumber) ? null : form.LicenseNumber.Trim();
      user.LicenseClass = string.IsNullOrWhiteSpace(form.LicenseClass) ? null : form.LicenseClass.Trim();
      user.LicenseExpiry = string.IsNullOrWhiteSpace(form.LicenseExpiry) ? null : form.LicenseExpiry.Trim();
      user.EmergencyContactName = form.EmergencyContactName.Trim();
      user.EmergencyContactRelation = form.EmergencyContactRelation.Trim();
      user.EmergencyContactPhone = form.EmergencyContactPhone.Trim();
      user.Address = form.Address.Trim();
      user.TwoFactorEnabled = form.TwoFactorEnabled;
      user.Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();

      if (form.AvatarFile is not null)
      {
        user.Avatar = await UserAssetStorage.SaveImageAsync(form.AvatarFile, user.Id, "avatar", environment);
      }

      if (form.NrcFrontFile is not null)
      {
        user.NrcFront = await UserAssetStorage.SaveImageAsync(form.NrcFrontFile, user.Id, "nrc-front", environment);
      }

      if (form.NrcBackFile is not null)
      {
        user.NrcBack = await UserAssetStorage.SaveImageAsync(form.NrcBackFile, user.Id, "nrc-back", environment);
      }

      if (string.IsNullOrWhiteSpace(user.Avatar) || string.IsNullOrWhiteSpace(user.NrcFront) || string.IsNullOrWhiteSpace(user.NrcBack))
      {
        return Results.BadRequest(new ApiError("Profile, NRC front, and NRC back images are required."));
      }

      user.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, request, "users", "Edit", user.Id, $"Updated user {user.Name}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToUserDto(user, roleEntity.Name, request));
    }).DisableAntiforgery();

    app.MapPatch("/api/users/{userId}/status", async (string userId, UserStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "users", PermissionAction.Edit);
      if (permissionError is not null) return permissionError;

      var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
      if (user is null) return Results.NotFound(new ApiError("User not found."));

      var oldStatus = user.Status;
      user.Status = string.IsNullOrWhiteSpace(request.Status) ? user.Status : request.Status.Trim();
      user.UpdatedAt = DateTime.UtcNow;
      AuditLogWriter.AddStatusHistoryIfChanged(db, httpRequest, "User", user.Id, oldStatus, user.Status);
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "users", "Edit", user.Id, $"Changed user status for {user.Name}.");
      await db.SaveChangesAsync();

      return Results.Ok(ToUserDto(user, user.Role!.Name, httpRequest));
    });

    app.MapDelete("/api/users/{userId}", async (string userId, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var permissionError = await PermissionChecks.RequirePermissionAsync(httpRequest, db, "users", PermissionAction.Delete);
      if (permissionError is not null) return permissionError;

      var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
      if (user is null) return Results.NotFound(new ApiError("User not found."));

      user.IsDeleted = 1;
      user.Status = "Disabled";
      user.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "users", "Delete", user.Id, $"Deleted user {user.Name}.");
      await db.SaveChangesAsync();
      return Results.NoContent();
    });

    app.MapGet("/api/profile", async (HttpRequest httpRequest, FleetDbContext db) =>
    {
      var user = await GetRequestUserAsync(httpRequest, db);
      if (user is null) return Results.Json(new ApiError("Login session is required."), statusCode: StatusCodes.Status401Unauthorized);
      return Results.Ok(ToUserDto(user, user.Role!.Name, httpRequest));
    });

    app.MapPost("/api/profile/change-password", async (ChangePasswordRequest request, HttpRequest httpRequest, FleetDbContext db) =>
    {
      var user = await GetRequestUserAsync(httpRequest, db);
      if (user is null) return Results.Json(new ApiError("Login session is required."), statusCode: StatusCodes.Status401Unauthorized);

      if (string.IsNullOrWhiteSpace(request.CurrentPassword)) return Results.BadRequest(new ApiError("Current password is required."));
      if (string.IsNullOrWhiteSpace(request.NewPassword)) return Results.BadRequest(new ApiError("New password is required."));
      if (request.NewPassword.Length < 8) return Results.BadRequest(new ApiError("New password must be at least 8 characters."));
      if (!request.NewPassword.Any(char.IsUpper) || !request.NewPassword.Any(char.IsDigit))
      {
        return Results.BadRequest(new ApiError("New password must include at least one uppercase letter and one number."));
      }
      if (request.NewPassword != request.ConfirmPassword) return Results.BadRequest(new ApiError("Password confirmation does not match."));
      if (!SeedData.VerifyPassword(request.CurrentPassword, user.PasswordHash)) return Results.BadRequest(new ApiError("Current password is incorrect."));

      user.PasswordHash = SeedData.HashPassword(request.NewPassword);
      user.UpdatedAt = DateTime.UtcNow;
      await AuditLogWriter.LogAuditAsync(db, httpRequest, "profile", "Edit", user.Id, $"Changed password for {user.Name}.");
      await db.SaveChangesAsync();

      return Results.Ok(new { message = "Password changed successfully." });
    });

    app.MapGet("/api/users/options", async (FleetDbContext db, string? role = null) =>
    {
      var query = db.Users
        .Include(user => user.Role)
        .Where(user =>
          user.IsDeleted == 0 &&
          user.Status == "Active" &&
          user.Role != null &&
          user.Role.IsDeleted == 0)
        .AsNoTracking()
        .AsQueryable();

      if (!string.IsNullOrWhiteSpace(role))
      {
        var normalizedRole = role.Trim().ToLower();
        query = query.Where(user => user.Role!.Name.ToLower() == normalizedRole);
      }

      var items = await query
        .OrderBy(user => user.Name)
        .Select(user => user.Name)
        .ToListAsync();

      return Results.Ok(items);
    });

    return app;
  }

  private static UserDto ToUserDto(User user, string roleName, HttpRequest request) =>
    new(
      user.Id,
      user.Name,
      user.EmployeeId,
      user.NrcNumber,
      user.Email,
      roleName,
      user.Status,
      user.Phone,
      PublicAssetUrls.ToPublicAssetUrl(request, user.Avatar),
      PublicAssetUrls.ToPublicAssetUrl(request, user.NrcFront),
      PublicAssetUrls.ToPublicAssetUrl(request, user.NrcBack),
      user.Department,
      user.Title,
      user.Location,
      user.Manager,
      user.LicenseNumber,
      user.LicenseClass,
      user.LicenseExpiry,
      user.EmergencyContactName,
      user.EmergencyContactRelation,
      user.EmergencyContactPhone,
      user.Address,
      user.TwoFactorEnabled,
      user.Notes,
      user.JoinDate,
      user.LastLogin);

  private static async Task<User?> GetRequestUserAsync(HttpRequest request, FleetDbContext db)
  {
    var userId = request.Headers["X-Fleet-User-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(userId)) return null;

    return await db.Users
      .Include(user => user.Role)
      .FirstOrDefaultAsync(user =>
        user.Id == userId &&
        user.IsDeleted == 0 &&
        user.Role != null &&
        user.Role.IsDeleted == 0);
  }

  private static string NextEmployeeId(IEnumerable<string> existingEmployeeIds)
  {
    var max = existingEmployeeIds
      .Select(value =>
      {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var normalized = value.Trim();
        if (normalized.StartsWith("EMP-", StringComparison.OrdinalIgnoreCase))
        {
          normalized = normalized[4..];
        }
        return int.TryParse(normalized, out var number) ? number : 0;
      })
      .DefaultIfEmpty(1000)
      .Max();

    return $"EMP-{max + 1:D4}";
  }
}
