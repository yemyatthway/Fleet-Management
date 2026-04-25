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
      ToPublicAssetUrl(request, u.Avatar)))
    .ToList();

  return Results.Ok(items);
});

app.MapPost("/api/roles", (RoleRequest request, FleetDbContext db) =>
{
  return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be created."));
});

app.MapPut("/api/roles/{roleId}", (string roleId, RoleRequest request, FleetDbContext db) =>
{
  return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be edited."));
});

app.MapDelete("/api/roles/{roleId}", (string roleId, FleetDbContext db) =>
{
  return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be deleted."));
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

app.MapGet("/api/departments", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? sortBy = "name",
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
    _ => query.OrderBy(department => department.Name)
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

app.MapPost("/api/departments", async (DepartmentRequest request, FleetDbContext db) =>
{
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
  await db.SaveChangesAsync();

  return Results.Ok(new DepartmentDto(
    department.Id,
    department.Name,
    department.Description,
    department.Status,
    department.CreatedAt,
    department.UpdatedAt));
});

app.MapPut("/api/departments/{id:int}", async (int id, DepartmentRequest request, FleetDbContext db) =>
{
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

  await db.SaveChangesAsync();

  return Results.Ok(new DepartmentDto(
    department.Id,
    department.Name,
    department.Description,
    department.Status,
    department.CreatedAt,
    department.UpdatedAt));
});

app.MapDelete("/api/departments/{id:int}", async (int id, FleetDbContext db) =>
{
  var department = await db.DepartmentCodeOptions.FirstOrDefaultAsync(item => item.Id == id);
  if (department is null) return Results.NotFound(new ApiError("Department not found."));

  var assignedUsers = await db.Users.CountAsync(user => user.IsDeleted == 0 && user.Department == department.Name);
  if (assignedUsers > 0)
  {
    return Results.BadRequest(new ApiError($"Cannot delete {department.Name} while users are assigned to it."));
  }

  db.DepartmentCodeOptions.Remove(department);
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapPost("/api/users", async (
  [FromForm] UserFormData form,
  HttpRequest request,
  IWebHostEnvironment environment,
  FleetDbContext db) =>
{
  var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == form.Role && r.IsDeleted == 0);
  if (roleEntity is null) return Results.BadRequest(new ApiError("Selected role does not exist."));

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
    Status = string.IsNullOrWhiteSpace(form.Status) ? "Active" : form.Status.Trim(),
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
  var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
  if (user is null) return Results.NotFound(new ApiError("User not found."));

  var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == form.Role && r.IsDeleted == 0);
  if (roleEntity is null) return Results.BadRequest(new ApiError("Selected role does not exist."));

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
  user.Status = string.IsNullOrWhiteSpace(form.Status) ? "Active" : form.Status.Trim();
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
  await db.SaveChangesAsync();

  return Results.Ok(ToUserDto(user, roleEntity.Name, request));
}).DisableAntiforgery();

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

app.MapGet("/api/locations", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? sortBy = "name",
  string? sortOrder = "asc") =>
{
  var query = db.LocationCodeOptions.AsNoTracking().AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(location =>
      location.Name.ToLower().Contains(normalizedSearch) ||
      location.Code.ToLower().Contains(normalizedSearch) ||
      location.Type.ToLower().Contains(normalizedSearch) ||
      location.Address.ToLower().Contains(normalizedSearch) ||
      location.City.ToLower().Contains(normalizedSearch) ||
      location.Country.ToLower().Contains(normalizedSearch) ||
      location.Phone.ToLower().Contains(normalizedSearch) ||
      (location.ContactPerson ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      (location.Notes ?? string.Empty).ToLower().Contains(normalizedSearch));
  }

  query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
  {
    ("id", "desc") => query.OrderByDescending(location => location.Id),
    ("id", _) => query.OrderBy(location => location.Id),
    ("code", "desc") => query.OrderByDescending(location => location.Code),
    ("code", _) => query.OrderBy(location => location.Code),
    ("type", "desc") => query.OrderByDescending(location => location.Type),
    ("type", _) => query.OrderBy(location => location.Type),
    ("city", "desc") => query.OrderByDescending(location => location.City),
    ("city", _) => query.OrderBy(location => location.City),
    ("country", "desc") => query.OrderByDescending(location => location.Country),
    ("country", _) => query.OrderBy(location => location.Country),
    ("status", "desc") => query.OrderByDescending(location => location.Status),
    ("status", _) => query.OrderBy(location => location.Status),
    ("createdat", "desc") => query.OrderByDescending(location => location.CreatedAt),
    ("createdat", _) => query.OrderBy(location => location.CreatedAt),
    ("updatedat", "desc") => query.OrderByDescending(location => location.UpdatedAt),
    ("updatedat", _) => query.OrderBy(location => location.UpdatedAt),
    ("name", "desc") => query.OrderByDescending(location => location.Name),
    _ => query.OrderBy(location => location.Name)
  };

  var total = await query.CountAsync();
  var items = await query
    .Skip(Math.Max(page - 1, 0) * pageSize)
    .Take(pageSize)
    .Select(location => new LocationDto(
      location.Id,
      location.Name,
      location.Code,
      location.Type,
      location.Address,
      location.City,
      location.Country,
      location.ContactPerson,
      location.Phone,
      location.OperatingHours,
      location.Notes,
      location.Status,
      location.CreatedAt,
      location.UpdatedAt))
    .ToListAsync();

  return Results.Ok(new PagedResult<LocationDto>(items, total));
});

app.MapGet("/api/locations/options", async (FleetDbContext db) =>
{
  var items = await db.LocationCodeOptions
    .AsNoTracking()
    .Where(location => location.Status == "Active")
    .OrderBy(location => location.Name)
    .Select(location => location.Name)
    .ToListAsync();

  return Results.Ok(items);
});

app.MapPost("/api/locations", async (LocationRequest request, FleetDbContext db) =>
{
  var validationError = ValidateLocationRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.LocationCodeOptions.AnyAsync(location =>
    location.Name.ToLower() == normalizedName.ToLower() ||
    location.Code.ToLower() == normalizedCode.ToLower());
  if (duplicateExists) return Results.BadRequest(new ApiError("Location name or code already exists."));

  var location = new LocationCodeOption
  {
    Name = normalizedName,
    Code = normalizedCode,
    Type = request.Type.Trim(),
    Address = request.Address.Trim(),
    City = request.City.Trim(),
    Country = request.Country.Trim(),
    ContactPerson = NormalizeOptional(request.ContactPerson),
    Phone = request.Phone.Trim(),
    OperatingHours = request.OperatingHours.Trim(),
    Notes = NormalizeOptional(request.Notes),
    Status = request.Status.Trim(),
    CreatedAt = DateTimeOffset.UtcNow
  };

  db.LocationCodeOptions.Add(location);
  await db.SaveChangesAsync();

  return Results.Ok(ToLocationDto(location));
});

app.MapPut("/api/locations/{id:int}", async (int id, LocationRequest request, FleetDbContext db) =>
{
  var validationError = ValidateLocationRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var location = await db.LocationCodeOptions.FindAsync(id);
  if (location is null) return Results.NotFound(new ApiError("Location not found."));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.LocationCodeOptions.AnyAsync(item =>
    item.Id != id &&
    (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
  if (duplicateExists) return Results.BadRequest(new ApiError("Location name or code already exists."));

  location.Name = normalizedName;
  location.Code = normalizedCode;
  location.Type = request.Type.Trim();
  location.Address = request.Address.Trim();
  location.City = request.City.Trim();
  location.Country = request.Country.Trim();
  location.ContactPerson = NormalizeOptional(request.ContactPerson);
  location.Phone = request.Phone.Trim();
  location.OperatingHours = request.OperatingHours.Trim();
  location.Notes = NormalizeOptional(request.Notes);
  location.Status = request.Status.Trim();
  location.UpdatedAt = DateTimeOffset.UtcNow;

  await db.SaveChangesAsync();

  return Results.Ok(ToLocationDto(location));
});

app.MapDelete("/api/locations/{id:int}", async (int id, FleetDbContext db) =>
{
  var location = await db.LocationCodeOptions.FindAsync(id);
  if (location is null) return Results.NotFound(new ApiError("Location not found."));

  db.LocationCodeOptions.Remove(location);
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/maintenance-tickets", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? status = null,
  string? sortBy = "reportedDate",
  string? sortOrder = "desc") =>
{
  var query = db.MaintenanceTickets
    .Where(ticket => ticket.IsDeleted == 0)
    .AsNoTracking()
    .AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(ticket =>
      ticket.Id.ToLower().Contains(normalizedSearch) ||
      ticket.Vehicle.ToLower().Contains(normalizedSearch) ||
      ticket.VehicleId.ToLower().Contains(normalizedSearch) ||
      ticket.Issue.ToLower().Contains(normalizedSearch) ||
      ticket.Mechanic.ToLower().Contains(normalizedSearch));
  }

  if (!string.IsNullOrWhiteSpace(status))
  {
    var normalizedStatus = status.Trim().ToLower();
    query = query.Where(ticket => ticket.Status.ToLower() == normalizedStatus);
  }

  query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
  {
    ("id", "asc") => query.OrderBy(ticket => ticket.Id),
    ("id", "desc") => query.OrderByDescending(ticket => ticket.Id),
    ("vehicle", "asc") => query.OrderBy(ticket => ticket.Vehicle),
    ("vehicle", "desc") => query.OrderByDescending(ticket => ticket.Vehicle),
    ("issue", "asc") => query.OrderBy(ticket => ticket.Issue),
    ("issue", "desc") => query.OrderByDescending(ticket => ticket.Issue),
    ("reporteddate", "asc") => query.OrderBy(ticket => ticket.ReportedDate),
    ("reporteddate", "desc") => query.OrderByDescending(ticket => ticket.ReportedDate),
    ("mechanic", "asc") => query.OrderBy(ticket => ticket.Mechanic),
    ("mechanic", "desc") => query.OrderByDescending(ticket => ticket.Mechanic),
    ("status", "asc") => query.OrderBy(ticket => ticket.Status),
    ("status", "desc") => query.OrderByDescending(ticket => ticket.Status),
    _ => query.OrderByDescending(ticket => ticket.ReportedDate)
  };

  var total = await query.CountAsync();
  var statsSource = db.MaintenanceTickets.Where(ticket => ticket.IsDeleted == 0);
  var stats = new MaintenanceTicketStatsDto(
    await statsSource.CountAsync(),
    await statsSource.CountAsync(ticket => ticket.Status == "Pending"),
    await statsSource.CountAsync(ticket => ticket.Status == "Repairing"),
    await statsSource.CountAsync(ticket => ticket.Status == "Completed"));

  var records = await query
    .Skip(Math.Max(page - 1, 0) * pageSize)
    .Take(pageSize)
    .ToListAsync();

  var items = records
    .Select(ToMaintenanceTicketDto)
    .ToList();

  return Results.Ok(new MaintenanceTicketPagedResult(items, total, stats));
});

app.MapPost("/api/maintenance-tickets", async (MaintenanceTicketRequest request, FleetDbContext db) =>
{
  var validationError = ValidateMaintenanceTicketRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var ticket = new MaintenanceTicket
  {
    Id = NextMaintenanceTicketId(await db.MaintenanceTickets.Select(item => item.Id).ToListAsync()),
    Vehicle = request.Vehicle.Trim(),
    VehicleId = request.VehicleId.Trim(),
    Issue = request.Issue.Trim(),
    Details = request.Details.Trim(),
    ReportedDate = request.ReportedDate.Trim(),
    Mechanic = request.Mechanic.Trim(),
    Status = request.Status.Trim(),
    IsDeleted = 0,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
  };

  db.MaintenanceTickets.Add(ticket);
  await db.SaveChangesAsync();

  return Results.Ok(ToMaintenanceTicketDto(ticket));
});

app.MapPut("/api/maintenance-tickets/{ticketId}", async (string ticketId, MaintenanceTicketRequest request, FleetDbContext db) =>
{
  var validationError = ValidateMaintenanceTicketRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
  if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

  ticket.Vehicle = request.Vehicle.Trim();
  ticket.VehicleId = request.VehicleId.Trim();
  ticket.Issue = request.Issue.Trim();
  ticket.Details = request.Details.Trim();
  ticket.ReportedDate = request.ReportedDate.Trim();
  ticket.Mechanic = request.Mechanic.Trim();
  ticket.Status = request.Status.Trim();
  ticket.UpdatedAt = DateTime.UtcNow;

  await db.SaveChangesAsync();

  return Results.Ok(ToMaintenanceTicketDto(ticket));
});

app.MapPatch("/api/maintenance-tickets/{ticketId}/status", async (string ticketId, MaintenanceTicketStatusRequest request, FleetDbContext db) =>
{
  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
  if (normalizedStatus is not ("Pending" or "Repairing" or "Completed"))
  {
    return Results.BadRequest(new ApiError("Ticket status must be Pending, Repairing, or Completed."));
  }

  var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
  if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

  ticket.Status = normalizedStatus;
  ticket.UpdatedAt = DateTime.UtcNow;
  await db.SaveChangesAsync();

  return Results.Ok(ToMaintenanceTicketDto(ticket));
});

app.MapDelete("/api/maintenance-tickets/{ticketId}", async (string ticketId, FleetDbContext db) =>
{
  var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
  if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

  ticket.IsDeleted = 1;
  ticket.UpdatedAt = DateTime.UtcNow;
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

static LocationDto ToLocationDto(LocationCodeOption location) =>
  new(
    location.Id,
    location.Name,
    location.Code,
    location.Type,
    location.Address,
    location.City,
    location.Country,
    location.ContactPerson,
    location.Phone,
    location.OperatingHours,
    location.Notes,
    location.Status,
    location.CreatedAt,
    location.UpdatedAt);

static MaintenanceTicketDto ToMaintenanceTicketDto(MaintenanceTicket ticket) =>
  new(
    ticket.Id,
    ticket.Vehicle,
    ticket.VehicleId,
    ticket.Issue,
    ticket.Details,
    ticket.ReportedDate,
    ticket.Mechanic,
    ticket.Status,
    ticket.CreatedAt,
    ticket.UpdatedAt);

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

static string NextMaintenanceTicketId(IEnumerable<string> existingIds)
{
  var max = existingIds
    .Select(value =>
    {
      var normalized = value.StartsWith("MT-", StringComparison.OrdinalIgnoreCase)
        ? value[3..]
        : value;
      return int.TryParse(normalized, out var number) ? number : 0;
    })
    .DefaultIfEmpty(2030)
    .Max();

  return $"MT-{max + 1}";
}

static string? ValidateLocationRequest(LocationRequest request)
{
  if (string.IsNullOrWhiteSpace(request.Name)) return "Location name is required.";
  if (string.IsNullOrWhiteSpace(request.Code)) return "Location code is required.";
  if (string.IsNullOrWhiteSpace(request.Type)) return "Location type is required.";
  if (string.IsNullOrWhiteSpace(request.Address)) return "Location address is required.";
  if (string.IsNullOrWhiteSpace(request.City)) return "Location city is required.";
  if (string.IsNullOrWhiteSpace(request.Country)) return "Location country is required.";
  if (string.IsNullOrWhiteSpace(request.Phone)) return "Location phone is required.";
  if (string.IsNullOrWhiteSpace(request.OperatingHours)) return "Operating hours are required.";
  if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
  return null;
}

static string? NormalizeOptional(string? value) =>
  string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static string? ValidateDepartmentRequest(DepartmentRequest request)
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

static string? ValidateMaintenanceTicketRequest(MaintenanceTicketRequest request)
{
  if (string.IsNullOrWhiteSpace(request.Vehicle)) return "Vehicle is required.";
  if (string.IsNullOrWhiteSpace(request.VehicleId)) return "Vehicle ID is required.";
  if (string.IsNullOrWhiteSpace(request.Issue)) return "Issue is required.";
  if (string.IsNullOrWhiteSpace(request.Details)) return "Details are required.";
  if (string.IsNullOrWhiteSpace(request.ReportedDate)) return "Reported date is required.";
  if (string.IsNullOrWhiteSpace(request.Mechanic)) return "Mechanic is required.";

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
  return normalizedStatus is "Pending" or "Repairing" or "Completed"
    ? null
    : "Ticket status must be Pending, Repairing, or Completed.";
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
