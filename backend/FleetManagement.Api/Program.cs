using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FleetDbContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("FleetDatabase")));

builder.Services.AddCors(options =>
{
  options.AddPolicy("frontend", policy =>
    policy
      .WithOrigins("http://localhost:5173")
      .AllowAnyHeader()
      .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("frontend");
app.UseStaticFiles();
var uploadsRoot = UserAssetStorage.GetUploadsRootPath(app.Environment);
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(uploadsRoot),
  RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
  var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
  await SchemaBootstrapper.EnsureRolesSchemaAsync(db);
  await SeedData.InitializeAsync(db);
  await UserAssetStorage.RepairStoredUserAssetPathsAsync(db, environment);
}

app.MapGet("/api/roles", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? role = null,
  string? sortBy = "id",
  string? sortOrder = "asc") =>
{
  var query = db.Roles.Where(r => r.IsDeleted == 0).AsQueryable();

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
  var items = await db.Roles
    .Where(r => r.IsDeleted == 0)
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
      ToPublicAssetUrl(request, u.Avatar)))
    .ToList();

  return Results.Ok(items);
});

app.MapPost("/api/roles", async (RoleRequest request, FleetDbContext db) =>
{
  var normalizedName = request.Name.Trim();
  var exists = await db.Roles.AnyAsync(r => r.IsDeleted == 0 && r.Name.ToLower() == normalizedName.ToLower());
  if (exists) return Results.BadRequest(new ApiError($"{normalizedName} already exists."));

  var now = DateTime.UtcNow;
  var role = new Role
  {
    Id = SeedData.ToSlug(normalizedName, await db.Roles.Select(r => r.Id).ToListAsync()),
    Code = SeedData.NextRoleCode(await db.Roles.Select(r => r.Code).ToListAsync()),
    Name = normalizedName,
    Description = request.Description.Trim(),
    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
    IsDeleted = 0,
    CreatedAt = now,
    UpdatedAt = now
  };

  db.Roles.Add(role);
  await db.SaveChangesAsync();

  return Results.Ok(new RoleDto(role.Id, role.Code, role.Name, role.Description, role.Status, 0, role.CreatedAt, role.UpdatedAt));
});

app.MapPut("/api/roles/{roleId}", async (string roleId, RoleRequest request, FleetDbContext db) =>
{
  var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.IsDeleted == 0);
  if (role is null) return Results.NotFound(new ApiError("Role not found."));

  var normalizedName = request.Name.Trim();
  var duplicate = await db.Roles.AnyAsync(r => r.Id != roleId && r.IsDeleted == 0 && r.Name.ToLower() == normalizedName.ToLower());
  if (duplicate) return Results.BadRequest(new ApiError($"{normalizedName} already exists."));

  role.Name = normalizedName;
  role.Description = request.Description.Trim();
  role.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  role.UpdatedAt = DateTime.UtcNow;

  await db.SaveChangesAsync();

  var memberCount = await db.Users.CountAsync(u => u.RoleId == role.Id && u.IsDeleted == 0);
  return Results.Ok(new RoleDto(role.Id, role.Code, role.Name, role.Description, role.Status, memberCount, role.CreatedAt, role.UpdatedAt));
});

app.MapDelete("/api/roles/{roleId}", async (string roleId, FleetDbContext db) =>
{
  var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.IsDeleted == 0);
  if (role is null) return Results.NotFound(new ApiError("Role not found."));
  var activeUsers = await db.Users.CountAsync(u => u.RoleId == roleId && u.IsDeleted == 0);
  if (activeUsers > 0)
  {
    return Results.BadRequest(new ApiError($"Cannot delete {role.Name} while users are assigned to it."));
  }

  role.IsDeleted = 1;
  role.UpdatedAt = DateTime.UtcNow;
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/users", async (
  HttpRequest request,
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? role = null,
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
  var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == form.Role && r.IsDeleted == 0);
  if (roleEntity is null) return Results.BadRequest(new ApiError("Selected role does not exist."));

  if (form.AvatarFile is null || form.NrcFrontFile is null || form.NrcBackFile is null)
  {
    return Results.BadRequest(new ApiError("Profile, NRC front, and NRC back images are required."));
  }

  var duplicateEmail = await db.Users.AnyAsync(u => u.IsDeleted == 0 && u.Email.ToLower() == form.Email.Trim().ToLower());
  if (duplicateEmail) return Results.BadRequest(new ApiError("Email already exists."));

  var now = DateTime.UtcNow;
  var existingIds = await db.Users
    .Select(u => u.Id)
    .ToListAsync();
  var nextId = existingIds
    .Select(id => int.TryParse(id, out var value) ? value : 0)
    .DefaultIfEmpty(0)
    .Max() + 1;
  var existingEmployeeIds = await db.Users
    .Select(u => u.EmployeeId)
    .ToListAsync();

  var user = new User
  {
    Id = nextId.ToString(),
    Name = form.Name.Trim(),
    EmployeeId = NextEmployeeId(existingEmployeeIds),
    NrcNumber = form.NrcNumber.Trim(),
    Email = form.Email.Trim(),
    RoleId = roleEntity.Id,
    Status = string.IsNullOrWhiteSpace(form.Status) ? "Active" : form.Status.Trim(),
    Phone = form.Phone.Trim(),
    Avatar = string.Empty,
    NrcFront = string.Empty,
    NrcBack = string.Empty,
    Department = form.Department.Trim(),
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
  await db.SaveChangesAsync();

  return Results.Ok(ToUserDto(user, roleEntity.Name, request));
})
.DisableAntiforgery();

app.MapPut("/api/users/{userId}", async (
  string userId,
  [FromForm] UserFormData form,
  HttpRequest request,
  IWebHostEnvironment environment,
  FleetDbContext db) =>
{
  var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
  if (user is null) return Results.NotFound(new ApiError("User not found."));

  var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == form.Role && r.IsDeleted == 0);
  if (roleEntity is null) return Results.BadRequest(new ApiError("Selected role does not exist."));

  var duplicateEmail = await db.Users.AnyAsync(u => u.Id != userId && u.IsDeleted == 0 && u.Email.ToLower() == form.Email.Trim().ToLower());
  if (duplicateEmail) return Results.BadRequest(new ApiError("Email already exists."));

  user.Name = form.Name.Trim();
  user.NrcNumber = form.NrcNumber.Trim();
  user.Email = form.Email.Trim();
  user.RoleId = roleEntity.Id;
  user.Status = string.IsNullOrWhiteSpace(form.Status) ? "Active" : form.Status.Trim();
  user.Phone = form.Phone.Trim();
  user.Department = form.Department.Trim();
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

  await db.SaveChangesAsync();

  return Results.Ok(ToUserDto(user, roleEntity.Name, request));
})
.DisableAntiforgery();

app.MapPatch("/api/users/{userId}/status", async (string userId, UserStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
  if (user is null) return Results.NotFound(new ApiError("User not found."));

  user.Status = string.IsNullOrWhiteSpace(request.Status) ? user.Status : request.Status.Trim();
  user.UpdatedAt = DateTime.UtcNow;
  await db.SaveChangesAsync();

  return Results.Ok(ToUserDto(user, user.Role!.Name, httpRequest));
});

app.MapDelete("/api/users/{userId}", async (string userId, FleetDbContext db) =>
{
  var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
  if (user is null) return Results.NotFound(new ApiError("User not found."));

  user.IsDeleted = 1;
  user.Status = "Disabled";
  user.UpdatedAt = DateTime.UtcNow;
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.Run();

static UserDto ToUserDto(User user, string roleName, HttpRequest request) =>
  new(
    user.Id,
    user.Name,
    user.EmployeeId,
    user.NrcNumber,
    user.Email,
    roleName,
    user.Status,
    user.Phone,
    ToPublicAssetUrl(request, user.Avatar),
    ToPublicAssetUrl(request, user.NrcFront),
    ToPublicAssetUrl(request, user.NrcBack),
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

static string NextEmployeeId(IEnumerable<string> existingEmployeeIds)
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

static string ToPublicAssetUrl(HttpRequest request, string? path)
{
  if (string.IsNullOrWhiteSpace(path)) return string.Empty;
  if (path.StartsWith("file:///uploads/", StringComparison.OrdinalIgnoreCase))
  {
    path = path.Replace("file://", "", StringComparison.OrdinalIgnoreCase);
  }
  if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri)) return absoluteUri.ToString();
  if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
  {
    path = $"/{path}";
  }
  if (!path.StartsWith('/')) return path;
  return $"{request.Scheme}://{request.Host}{path}";
}
