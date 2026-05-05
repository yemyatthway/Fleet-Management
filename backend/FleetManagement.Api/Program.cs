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
      ToPublicAssetUrl(request, u.Avatar)))
    .ToList();

  return Results.Ok(items);
});

app.MapPost("/api/roles", async (RoleRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "roles", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be created."));
});

app.MapPut("/api/roles/{roleId}", async (string roleId, RoleRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "roles", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be edited."));
});

app.MapDelete("/api/roles/{roleId}", async (string roleId, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "roles", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  return Results.BadRequest(new ApiError("Roles are fixed system roles and cannot be deleted."));
});

app.MapGet("/api/permissions", async (FleetDbContext db) =>
{
  return Results.Ok(await BuildPermissionMatrixAsync(db));
});

app.MapPut("/api/permissions", async (PermissionBulkUpdateRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "permissions", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var fixedRoleIds = SeedData.FixedRoleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
  var moduleKeys = GetPermissionModules().Select(module => module.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var submitted = request.Permissions ?? [];

  foreach (var permission in submitted)
  {
    if (!fixedRoleIds.Contains(permission.RoleId))
    {
      return Results.BadRequest(new ApiError("Permission role is not a fixed system role."));
    }

    if (!moduleKeys.Contains(permission.ModuleKey))
    {
      return Results.BadRequest(new ApiError("Permission module does not exist."));
    }
  }

  var now = DateTime.UtcNow;
  foreach (var permission in submitted)
  {
    var normalizedRoleId = permission.RoleId.Trim();
    var normalizedModuleKey = permission.ModuleKey.Trim();
    var existingPermission = await db.RolePermissions.FirstOrDefaultAsync(item =>
      item.RoleId == normalizedRoleId && item.ModuleKey == normalizedModuleKey);

    if (existingPermission is null)
    {
      db.RolePermissions.Add(new RolePermission
      {
        RoleId = normalizedRoleId,
        ModuleKey = normalizedModuleKey,
        CanView = permission.CanView,
        CanCreate = permission.CanCreate,
        CanEdit = permission.CanEdit,
        CanDelete = permission.CanDelete,
        CreatedAt = now,
        UpdatedAt = now
      });
      continue;
    }

    existingPermission.CanView = permission.CanView;
    existingPermission.CanCreate = permission.CanCreate;
    existingPermission.CanEdit = permission.CanEdit;
    existingPermission.CanDelete = permission.CanDelete;
    existingPermission.UpdatedAt = now;
  }

  await db.SaveChangesAsync();
  await LogAuditAsync(db, httpRequest, "permissions", "Edit", "matrix", "Updated role permission matrix.");
  await db.SaveChangesAsync();
  return Results.Ok(await BuildPermissionMatrixAsync(db));
});

app.MapGet("/api/dashboard/summary", async (FleetDbContext db) =>
{
  var vehicles = db.Vehicles.Where(vehicle => vehicle.IsDeleted == 0);
  var trips = db.Trips.Where(trip => trip.IsDeleted == 0);
  var tickets = db.MaintenanceTickets.Where(ticket => ticket.IsDeleted == 0);
  var incidents = db.Incidents.Where(incident => incident.IsDeleted == 0);

  var vehicleStatuses = await SafeDashboardValueAsync(async () => await vehicles
    .GroupBy(vehicle => vehicle.Status)
    .Select(group => new NamedCountDto(group.Key, group.Count()))
    .OrderByDescending(item => item.Value)
    .ToListAsync(), new List<NamedCountDto>());

  var tripStatuses = await SafeDashboardValueAsync(async () => await trips
    .GroupBy(trip => trip.Status)
    .Select(group => new NamedCountDto(group.Key, group.Count()))
    .OrderByDescending(item => item.Value)
    .ToListAsync(), new List<NamedCountDto>());

  var recentTripRows = await SafeDashboardValueAsync(async () => await trips
    .OrderByDescending(trip => trip.UpdatedAt)
    .ThenByDescending(trip => trip.Id)
    .Take(8)
    .Select(trip => new
    {
      trip.Id,
      trip.TripNumber,
      trip.VehiclePlate,
      trip.DriverName,
      trip.PickupLocation,
      trip.DropoffLocation,
      trip.Status,
      trip.TripType,
      trip.Priority
    })
    .ToListAsync(), []);

  var recentTrips = recentTripRows
    .Select(trip => new DashboardRecentTripDto(
      trip.Id,
      trip.TripNumber,
      trip.VehiclePlate,
      trip.DriverName,
      $"{trip.PickupLocation} to {trip.DropoffLocation}",
      trip.Status,
      string.IsNullOrWhiteSpace(trip.TripType) && string.IsNullOrWhiteSpace(trip.Priority)
        ? "-"
        : $"{trip.TripType} | {trip.Priority}"))
    .ToList();

  var upcomingExpiries = (await SafeDashboardValueAsync(
      async () => await vehicles.AsNoTracking().ToListAsync(),
      new List<Vehicle>()))
    .SelectMany(GetUpcomingVehicleExpiries)
    .OrderBy(expiry => expiry.DaysRemaining)
    .Take(8)
    .ToList();

  var metrics = new List<DashboardMetricDto>
  {
    new("Total Vehicles", await SafeDashboardValueAsync(() => vehicles.CountAsync(), 0), "mdi-truck", "info"),
    new("Active Trips", await SafeDashboardValueAsync(() => trips.CountAsync(trip => trip.Status == "In Transit" || trip.Status == "Active" || trip.Status == "Ongoing"), 0), "mdi-map-marker", "success"),
    new("Open Maintenance", await SafeDashboardValueAsync(() => tickets.CountAsync(ticket => ticket.Status != "Completed" && ticket.Status != "Closed"), 0), "mdi-wrench", "warning"),
    new("Incidents", await SafeDashboardValueAsync(() => incidents.CountAsync(), 0), "mdi-alert-circle-outline", "danger"),
    new("Upcoming Expiries", upcomingExpiries.Count, "mdi-calendar-alert", "purple")
  };

  return Results.Ok(new DashboardSummaryDto(metrics, vehicleStatuses, tripStatuses, recentTrips, upcomingExpiries));
});

app.MapPost("/api/auth/login", async (LoginRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
  {
    return Results.BadRequest(new ApiError("Email and password are required."));
  }

  var normalizedEmail = request.Email.Trim().ToLower();
  var user = await db.Users
    .Include(item => item.Role)
    .FirstOrDefaultAsync(item =>
      item.IsDeleted == 0 &&
      item.Email.ToLower() == normalizedEmail &&
      item.Role != null &&
      item.Role.IsDeleted == 0);

  if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !SeedData.VerifyPassword(request.Password, user.PasswordHash))
  {
    return Results.BadRequest(new ApiError("Invalid email or password."));
  }

  if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
  {
    return Results.BadRequest(new ApiError("This user account is not active."));
  }

  user.LastLogin = DateTime.UtcNow.ToString("o");
  user.UpdatedAt = DateTime.UtcNow;
  await db.SaveChangesAsync();

  var permissions = await GetPermissionsForRoleAsync(db, user.RoleId);
  return Results.Ok(new LoginResponseDto(
    new AuthUserDto(
      user.Id,
      user.Name,
      user.Email,
      user.RoleId,
      user.Role!.Name,
      user.Status,
      ToPublicAssetUrl(httpRequest, user.Avatar)),
    permissions));
});

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
  var permissionError = await RequirePermissionAsync(httpRequest, db, "department-setup", PermissionAction.Create);
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
  await LogAuditAsync(db, httpRequest, "department-setup", "Create", normalizedName, $"Created department {normalizedName}.");
  await db.SaveChangesAsync();

  return Results.Ok(new DepartmentDto(
    department.Id,
    department.Name,
    department.Description,
    department.Status,
    department.CreatedAt,
    department.UpdatedAt));
});

app.MapPut("/api/departments/{id:int}", async (int id, DepartmentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "department-setup", PermissionAction.Edit);
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

  await LogAuditAsync(db, httpRequest, "department-setup", "Edit", id.ToString(), $"Updated department {normalizedName}.");
  await db.SaveChangesAsync();

  return Results.Ok(new DepartmentDto(
    department.Id,
    department.Name,
    department.Description,
    department.Status,
    department.CreatedAt,
    department.UpdatedAt));
});

app.MapDelete("/api/departments/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "department-setup", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var department = await db.DepartmentCodeOptions.FirstOrDefaultAsync(item => item.Id == id);
  if (department is null) return Results.NotFound(new ApiError("Department not found."));

  var assignedUsers = await db.Users.CountAsync(user => user.IsDeleted == 0 && user.Department == department.Name);
  if (assignedUsers > 0)
  {
    return Results.BadRequest(new ApiError($"Cannot delete {department.Name} while users are assigned to it."));
  }

  db.DepartmentCodeOptions.Remove(department);
  await LogAuditAsync(db, httpRequest, "department-setup", "Delete", id.ToString(), $"Deleted department {department.Name}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapPost("/api/users", async (
  [FromForm] UserFormData form,
  HttpRequest request,
  IWebHostEnvironment environment,
  FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(request, db, "users", PermissionAction.Create);
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
  await LogAuditAsync(db, request, "users", "Create", user.Id, $"Created user {user.Name}.");
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
  var permissionError = await RequirePermissionAsync(request, db, "users", PermissionAction.Edit);
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
  await LogAuditAsync(db, request, "users", "Edit", user.Id, $"Updated user {user.Name}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToUserDto(user, roleEntity.Name, request));
}).DisableAntiforgery();

app.MapPatch("/api/users/{userId}/status", async (string userId, UserStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "users", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
  if (user is null) return Results.NotFound(new ApiError("User not found."));

  var oldStatus = user.Status;
  user.Status = string.IsNullOrWhiteSpace(request.Status) ? user.Status : request.Status.Trim();
  user.UpdatedAt = DateTime.UtcNow;
  AddStatusHistoryIfChanged(db, httpRequest, "User", user.Id, oldStatus, user.Status);
  await LogAuditAsync(db, httpRequest, "users", "Edit", user.Id, $"Changed user status for {user.Name}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToUserDto(user, user.Role!.Name, httpRequest));
});

app.MapDelete("/api/users/{userId}", async (string userId, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "users", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == 0);
  if (user is null) return Results.NotFound(new ApiError("User not found."));

  user.IsDeleted = 1;
  user.Status = "Disabled";
  user.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "users", "Delete", user.Id, $"Deleted user {user.Name}.");
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
  string? sortBy = "id",
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
    ("name", _) => query.OrderBy(location => location.Name),
    _ => query.OrderBy(location => location.Id)
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

app.MapPost("/api/locations", async (LocationRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "location-setup", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

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

app.MapPut("/api/locations/{id:int}", async (int id, LocationRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "location-setup", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

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

app.MapDelete("/api/locations/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "location-setup", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var location = await db.LocationCodeOptions.FindAsync(id);
  if (location is null) return Results.NotFound(new ApiError("Location not found."));

  db.LocationCodeOptions.Remove(location);
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/location-types", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? sortBy = "id",
  string? sortOrder = "asc") =>
{
  var query = db.LocationTypeCodeOptions.AsNoTracking().AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(locationType =>
      locationType.Name.ToLower().Contains(normalizedSearch) ||
      locationType.Code.ToLower().Contains(normalizedSearch) ||
      (locationType.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      locationType.Status.ToLower().Contains(normalizedSearch));
  }

  query = sortOrder?.ToLowerInvariant() == "desc"
    ? query.OrderByDescending(locationType => locationType.Id)
    : query.OrderBy(locationType => locationType.Id);

  var total = await query.CountAsync();
  var items = await query
    .Skip(Math.Max(page - 1, 0) * pageSize)
    .Take(pageSize)
    .Select(locationType => new LocationTypeDto(
      locationType.Id,
      locationType.Name,
      locationType.Code,
      locationType.Description,
      locationType.Status,
      locationType.CreatedAt,
      locationType.UpdatedAt))
    .ToListAsync();

  return Results.Ok(new PagedResult<LocationTypeDto>(items, total));
});

app.MapGet("/api/location-types/options", async (FleetDbContext db) =>
{
  var items = await db.LocationTypeCodeOptions
    .AsNoTracking()
    .Where(locationType => locationType.Status == "Active")
    .OrderBy(locationType => locationType.Id)
    .Select(locationType => locationType.Name)
    .ToListAsync();

  return Results.Ok(items);
});

app.MapPost("/api/location-types", async (LocationTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "location-type-setup", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateLocationTypeRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.LocationTypeCodeOptions.AnyAsync(locationType =>
    locationType.Name.ToLower() == normalizedName.ToLower() ||
    locationType.Code.ToLower() == normalizedCode.ToLower());
  if (duplicateExists) return Results.BadRequest(new ApiError("Location type name or code already exists."));

  var now = DateTimeOffset.UtcNow;
  var locationType = new LocationTypeCodeOption
  {
    Name = normalizedName,
    Code = normalizedCode,
    Description = NormalizeOptional(request.Description),
    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
    CreatedAt = now,
    UpdatedAt = now
  };

  db.LocationTypeCodeOptions.Add(locationType);
  await db.SaveChangesAsync();

  return Results.Ok(ToLocationTypeDto(locationType));
});

app.MapPut("/api/location-types/{id:int}", async (int id, LocationTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "location-type-setup", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateLocationTypeRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var locationType = await db.LocationTypeCodeOptions.FindAsync(id);
  if (locationType is null) return Results.NotFound(new ApiError("Location type not found."));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.LocationTypeCodeOptions.AnyAsync(item =>
    item.Id != id &&
    (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
  if (duplicateExists) return Results.BadRequest(new ApiError("Location type name or code already exists."));

  locationType.Name = normalizedName;
  locationType.Code = normalizedCode;
  locationType.Description = NormalizeOptional(request.Description);
  locationType.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  locationType.UpdatedAt = DateTimeOffset.UtcNow;

  await db.SaveChangesAsync();

  return Results.Ok(ToLocationTypeDto(locationType));
});

app.MapDelete("/api/location-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "location-type-setup", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var locationType = await db.LocationTypeCodeOptions.FindAsync(id);
  if (locationType is null) return Results.NotFound(new ApiError("Location type not found."));

  db.LocationTypeCodeOptions.Remove(locationType);
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/vehicle-types", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? sortBy = "id",
  string? sortOrder = "asc") =>
{
  var query = db.VehicleTypeCodeOptions.AsNoTracking().AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(vehicleType =>
      vehicleType.Name.ToLower().Contains(normalizedSearch) ||
      vehicleType.Code.ToLower().Contains(normalizedSearch) ||
      (vehicleType.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      vehicleType.Status.ToLower().Contains(normalizedSearch));
  }

  query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
  {
    ("id", "desc") => query.OrderByDescending(vehicleType => vehicleType.Id),
    ("id", _) => query.OrderBy(vehicleType => vehicleType.Id),
    ("code", "desc") => query.OrderByDescending(vehicleType => vehicleType.Code),
    ("code", _) => query.OrderBy(vehicleType => vehicleType.Code),
    ("description", "desc") => query.OrderByDescending(vehicleType => vehicleType.Description),
    ("description", _) => query.OrderBy(vehicleType => vehicleType.Description),
    ("status", "desc") => query.OrderByDescending(vehicleType => vehicleType.Status),
    ("status", _) => query.OrderBy(vehicleType => vehicleType.Status),
    ("createdat", "desc") => query.OrderByDescending(vehicleType => vehicleType.CreatedAt),
    ("createdat", _) => query.OrderBy(vehicleType => vehicleType.CreatedAt),
    ("updatedat", "desc") => query.OrderByDescending(vehicleType => vehicleType.UpdatedAt),
    ("updatedat", _) => query.OrderBy(vehicleType => vehicleType.UpdatedAt),
    ("name", "desc") => query.OrderByDescending(vehicleType => vehicleType.Name),
    ("name", _) => query.OrderBy(vehicleType => vehicleType.Name),
    _ => query.OrderBy(vehicleType => vehicleType.Id)
  };

  var total = await query.CountAsync();
  var items = await query
    .Skip(Math.Max(page - 1, 0) * pageSize)
    .Take(pageSize)
    .Select(vehicleType => new VehicleTypeDto(
      vehicleType.Id,
      vehicleType.Name,
      vehicleType.Code,
      vehicleType.Description,
      vehicleType.Status,
      vehicleType.CreatedAt,
      vehicleType.UpdatedAt))
    .ToListAsync();

  return Results.Ok(new PagedResult<VehicleTypeDto>(items, total));
});

app.MapGet("/api/vehicle-types/options", async (FleetDbContext db) =>
{
  var items = await db.VehicleTypeCodeOptions
    .AsNoTracking()
    .Where(vehicleType => vehicleType.Status == "Active")
    .OrderBy(vehicleType => vehicleType.Name)
    .Select(vehicleType => vehicleType.Name)
    .ToListAsync();

  return Results.Ok(items);
});

app.MapPost("/api/vehicle-types", async (VehicleTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "vehicle-type-setup", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateVehicleTypeRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.VehicleTypeCodeOptions.AnyAsync(vehicleType =>
    vehicleType.Name.ToLower() == normalizedName.ToLower() ||
    vehicleType.Code.ToLower() == normalizedCode.ToLower());
  if (duplicateExists) return Results.BadRequest(new ApiError("Vehicle type name or code already exists."));

  var now = DateTimeOffset.UtcNow;
  var vehicleType = new VehicleTypeCodeOption
  {
    Name = normalizedName,
    Code = normalizedCode,
    Description = NormalizeOptional(request.Description),
    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
    CreatedAt = now,
    UpdatedAt = now
  };

  db.VehicleTypeCodeOptions.Add(vehicleType);
  await db.SaveChangesAsync();

  return Results.Ok(ToVehicleTypeDto(vehicleType));
});

app.MapPut("/api/vehicle-types/{id:int}", async (int id, VehicleTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "vehicle-type-setup", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateVehicleTypeRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var vehicleType = await db.VehicleTypeCodeOptions.FindAsync(id);
  if (vehicleType is null) return Results.NotFound(new ApiError("Vehicle type not found."));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.VehicleTypeCodeOptions.AnyAsync(item =>
    item.Id != id &&
    (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
  if (duplicateExists) return Results.BadRequest(new ApiError("Vehicle type name or code already exists."));

  vehicleType.Name = normalizedName;
  vehicleType.Code = normalizedCode;
  vehicleType.Description = NormalizeOptional(request.Description);
  vehicleType.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  vehicleType.UpdatedAt = DateTimeOffset.UtcNow;

  await db.SaveChangesAsync();

  return Results.Ok(ToVehicleTypeDto(vehicleType));
});

app.MapDelete("/api/vehicle-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "vehicle-type-setup", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var vehicleType = await db.VehicleTypeCodeOptions.FindAsync(id);
  if (vehicleType is null) return Results.NotFound(new ApiError("Vehicle type not found."));

  db.VehicleTypeCodeOptions.Remove(vehicleType);
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/fuel-types", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? sortBy = "id",
  string? sortOrder = "asc") =>
{
  var query = db.FuelTypeCodeOptions.AsNoTracking().AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(fuelType =>
      fuelType.Name.ToLower().Contains(normalizedSearch) ||
      fuelType.Code.ToLower().Contains(normalizedSearch) ||
      (fuelType.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      fuelType.Status.ToLower().Contains(normalizedSearch));
  }

  query = sortOrder?.ToLowerInvariant() == "desc"
    ? query.OrderByDescending(fuelType => fuelType.Id)
    : query.OrderBy(fuelType => fuelType.Id);

  var total = await query.CountAsync();
  var items = await query
    .Skip(Math.Max(page - 1, 0) * pageSize)
    .Take(pageSize)
    .Select(fuelType => new FuelTypeDto(
      fuelType.Id,
      fuelType.Name,
      fuelType.Code,
      fuelType.Description,
      fuelType.Status,
      fuelType.CreatedAt,
      fuelType.UpdatedAt))
    .ToListAsync();

  return Results.Ok(new PagedResult<FuelTypeDto>(items, total));
});

app.MapGet("/api/fuel-types/options", async (FleetDbContext db) =>
{
  var items = await db.FuelTypeCodeOptions
    .AsNoTracking()
    .Where(fuelType => fuelType.Status == "Active")
    .OrderBy(fuelType => fuelType.Id)
    .Select(fuelType => fuelType.Name)
    .ToListAsync();

  return Results.Ok(items);
});

app.MapPost("/api/fuel-types", async (FuelTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "fuel-type-setup", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateFuelTypeRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.FuelTypeCodeOptions.AnyAsync(fuelType =>
    fuelType.Name.ToLower() == normalizedName.ToLower() ||
    fuelType.Code.ToLower() == normalizedCode.ToLower());
  if (duplicateExists) return Results.BadRequest(new ApiError("Fuel type name or code already exists."));

  var now = DateTimeOffset.UtcNow;
  var fuelType = new FuelTypeCodeOption
  {
    Name = normalizedName,
    Code = normalizedCode,
    Description = NormalizeOptional(request.Description),
    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
    CreatedAt = now,
    UpdatedAt = now
  };

  db.FuelTypeCodeOptions.Add(fuelType);
  await db.SaveChangesAsync();

  return Results.Ok(ToFuelTypeDto(fuelType));
});

app.MapPut("/api/fuel-types/{id:int}", async (int id, FuelTypeRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "fuel-type-setup", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateFuelTypeRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var fuelType = await db.FuelTypeCodeOptions.FindAsync(id);
  if (fuelType is null) return Results.NotFound(new ApiError("Fuel type not found."));

  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicateExists = await db.FuelTypeCodeOptions.AnyAsync(item =>
    item.Id != id &&
    (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
  if (duplicateExists) return Results.BadRequest(new ApiError("Fuel type name or code already exists."));

  fuelType.Name = normalizedName;
  fuelType.Code = normalizedCode;
  fuelType.Description = NormalizeOptional(request.Description);
  fuelType.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  fuelType.UpdatedAt = DateTimeOffset.UtcNow;

  await db.SaveChangesAsync();

  return Results.Ok(ToFuelTypeDto(fuelType));
});

app.MapDelete("/api/fuel-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "fuel-type-setup", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var fuelType = await db.FuelTypeCodeOptions.FindAsync(id);
  if (fuelType is null) return Results.NotFound(new ApiError("Fuel type not found."));

  db.FuelTypeCodeOptions.Remove(fuelType);
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/trip-types", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<TripTypeCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/trip-types/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<TripTypeCodeOption>(db)));
app.MapPost("/api/trip-types", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<TripTypeCodeOption>(request, httpRequest, db, "trip-type-setup"));
app.MapPut("/api/trip-types/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<TripTypeCodeOption>(id, request, httpRequest, db, "trip-type-setup"));
app.MapDelete("/api/trip-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<TripTypeCodeOption>(id, httpRequest, db, "trip-type-setup"));

app.MapGet("/api/cargo-types", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<CargoTypeCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/cargo-types/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<CargoTypeCodeOption>(db)));
app.MapPost("/api/cargo-types", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<CargoTypeCodeOption>(request, httpRequest, db, "cargo-type-setup"));
app.MapPut("/api/cargo-types/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<CargoTypeCodeOption>(id, request, httpRequest, db, "cargo-type-setup"));
app.MapDelete("/api/cargo-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<CargoTypeCodeOption>(id, httpRequest, db, "cargo-type-setup"));

app.MapGet("/api/statuses", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<StatusCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/statuses/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<StatusCodeOption>(db)));
app.MapPost("/api/statuses", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<StatusCodeOption>(request, httpRequest, db, "status-setup"));
app.MapPut("/api/statuses/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<StatusCodeOption>(id, request, httpRequest, db, "status-setup"));
app.MapDelete("/api/statuses/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<StatusCodeOption>(id, httpRequest, db, "status-setup"));

app.MapGet("/api/trip-priorities", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<TripPriorityCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/trip-priorities/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<TripPriorityCodeOption>(db)));
app.MapPost("/api/trip-priorities", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<TripPriorityCodeOption>(request, httpRequest, db, "trip-priority-setup"));
app.MapPut("/api/trip-priorities/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<TripPriorityCodeOption>(id, request, httpRequest, db, "trip-priority-setup"));
app.MapDelete("/api/trip-priorities/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<TripPriorityCodeOption>(id, httpRequest, db, "trip-priority-setup"));

app.MapGet("/api/incident-types", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<IncidentTypeCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/incident-types/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<IncidentTypeCodeOption>(db)));
app.MapPost("/api/incident-types", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<IncidentTypeCodeOption>(request, httpRequest, db, "incident-type-setup"));
app.MapPut("/api/incident-types/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<IncidentTypeCodeOption>(id, request, httpRequest, db, "incident-type-setup"));
app.MapDelete("/api/incident-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<IncidentTypeCodeOption>(id, httpRequest, db, "incident-type-setup"));

app.MapGet("/api/severities", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<SeverityCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/severities/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<SeverityCodeOption>(db)));
app.MapPost("/api/severities", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<SeverityCodeOption>(request, httpRequest, db, "severity-setup"));
app.MapPut("/api/severities/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<SeverityCodeOption>(id, request, httpRequest, db, "severity-setup"));
app.MapDelete("/api/severities/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<SeverityCodeOption>(id, httpRequest, db, "severity-setup"));

app.MapGet("/api/expense-types", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<ExpenseTypeCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/expense-types/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<ExpenseTypeCodeOption>(db)));
app.MapPost("/api/expense-types", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<ExpenseTypeCodeOption>(request, httpRequest, db, "expense-type-setup"));
app.MapPut("/api/expense-types/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<ExpenseTypeCodeOption>(id, request, httpRequest, db, "expense-type-setup"));
app.MapDelete("/api/expense-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<ExpenseTypeCodeOption>(id, httpRequest, db, "expense-type-setup"));

app.MapGet("/api/maintenance-types", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<MaintenanceTypeCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/maintenance-types/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<MaintenanceTypeCodeOption>(db)));
app.MapPost("/api/maintenance-types", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<MaintenanceTypeCodeOption>(request, httpRequest, db, "maintenance-type-setup"));
app.MapPut("/api/maintenance-types/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<MaintenanceTypeCodeOption>(id, request, httpRequest, db, "maintenance-type-setup"));
app.MapDelete("/api/maintenance-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<MaintenanceTypeCodeOption>(id, httpRequest, db, "maintenance-type-setup"));

app.MapGet("/api/document-types", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<DocumentTypeCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/document-types/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<DocumentTypeCodeOption>(db)));
app.MapPost("/api/document-types", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<DocumentTypeCodeOption>(request, httpRequest, db, "document-type-setup"));
app.MapPut("/api/document-types/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<DocumentTypeCodeOption>(id, request, httpRequest, db, "document-type-setup"));
app.MapDelete("/api/document-types/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<DocumentTypeCodeOption>(id, httpRequest, db, "document-type-setup"));

app.MapGet("/api/suppliers", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = "id", string? sortOrder = "asc") =>
  Results.Ok(await GetTripSetupPage<SupplierCodeOption>(db, page, pageSize, search, sortBy, sortOrder)));
app.MapGet("/api/suppliers/options", async (FleetDbContext db) => Results.Ok(await GetTripSetupOptions<SupplierCodeOption>(db)));
app.MapPost("/api/suppliers", async (TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await CreateTripSetupOption<SupplierCodeOption>(request, httpRequest, db, "supplier-setup"));
app.MapPut("/api/suppliers/{id:int}", async (int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db) => await UpdateTripSetupOption<SupplierCodeOption>(id, request, httpRequest, db, "supplier-setup"));
app.MapDelete("/api/suppliers/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) => await DeleteTripSetupOption<SupplierCodeOption>(id, httpRequest, db, "supplier-setup"));

app.MapGet("/api/vehicles", async (
  HttpRequest request,
  FleetDbContext db,
  string? search = null,
  string? status = null) =>
{
  var query = db.Vehicles
    .Where(vehicle => vehicle.IsDeleted == 0)
    .AsNoTracking()
    .AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(vehicle =>
      vehicle.Id.ToLower().Contains(normalizedSearch) ||
      vehicle.Plate.ToLower().Contains(normalizedSearch) ||
      vehicle.Driver.ToLower().Contains(normalizedSearch) ||
      vehicle.Type.ToLower().Contains(normalizedSearch) ||
      vehicle.Model.ToLower().Contains(normalizedSearch));
  }

  if (!string.IsNullOrWhiteSpace(status) && status != "All")
  {
    var normalizedStatus = status.Trim().ToLower();
    query = query.Where(vehicle => vehicle.Status.ToLower() == normalizedStatus);
  }

  var records = await query
    .OrderBy(vehicle => vehicle.Id)
    .ToListAsync();

  return Results.Ok(records.Select(vehicle => ToVehicleDto(vehicle, request)).ToList());
});

app.MapPost("/api/vehicles", async (
  [FromForm] VehicleFormData form,
  HttpRequest request,
  IWebHostEnvironment environment,
  FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(request, db, "vehicles", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateVehicleRequest(form);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var normalizedPlate = form.Plate!.Trim().ToUpperInvariant();
  var duplicatePlate = await db.Vehicles.AnyAsync(vehicle =>
    vehicle.IsDeleted == 0 && vehicle.Plate.ToLower() == normalizedPlate.ToLower());
  if (duplicatePlate) return Results.BadRequest(new ApiError("Vehicle plate already exists."));

  var now = DateTime.UtcNow;
  var vehicle = new Vehicle
  {
    Id = NextVehicleId(await db.Vehicles.Select(item => item.Id).ToListAsync()),
    Plate = normalizedPlate,
    Region = form.Region!.Trim(),
    Type = form.Type!.Trim(),
    Model = form.Model!.Trim(),
    Make = NormalizeOptional(form.Make),
    Year = NormalizeOptional(form.Year),
    Color = NormalizeOptional(form.Color),
    Status = form.Status!.Trim(),
    Ownership = NormalizeOptional(form.Ownership) ?? "Owned",
    Driver = form.Driver!.Trim(),
    DriverImage = string.Empty,
    Depot = NormalizeOptional(form.Depot),
    Capacity = NormalizeOptional(form.Capacity),
    FuelCapacity = NormalizeOptional(form.FuelCapacity),
    FuelType = form.FuelType!.Trim(),
    Vin = NormalizeOptional(form.Vin),
    EngineNo = NormalizeOptional(form.EngineNo),
    Odometer = NormalizeOptional(form.Odometer),
    LastService = NormalizeOptional(form.LastService),
    NextService = NormalizeOptional(form.NextService),
    ServiceNote = NormalizeOptional(form.ServiceNote),
    PurchaseCost = NormalizeOptional(form.PurchaseCost),
    RegistrationNo = NormalizeOptional(form.RegistrationNo),
    RegistrationExpiry = NormalizeOptional(form.RegistrationExpiry),
    RoadTaxExpiry = NormalizeOptional(form.RoadTaxExpiry),
    InsuranceExpiry = NormalizeOptional(form.InsuranceExpiry),
    InsuranceProvider = NormalizeOptional(form.InsuranceProvider),
    InsurancePolicy = NormalizeOptional(form.InsurancePolicy),
    InspectionDue = NormalizeOptional(form.InspectionDue),
    AcquiredDate = NormalizeOptional(form.AcquiredDate),
    Image = string.Empty,
    IsDeleted = 0,
    CreatedAt = now,
    UpdatedAt = now
  };

  db.Vehicles.Add(vehicle);
  await db.SaveChangesAsync();

  if (form.DriverImageFile is null && !form.RemoveDriverImage)
  {
    var driverAvatar = await db.Users
      .Include(user => user.Role)
      .Where(user =>
        user.IsDeleted == 0 &&
        user.Status == "Active" &&
        user.Name == vehicle.Driver &&
        user.Role != null &&
        user.Role.Name == "Driver")
      .Select(user => user.Avatar)
      .FirstOrDefaultAsync();

    vehicle.DriverImage = NormalizeOptional(driverAvatar);
  }

  if (form.VehicleImageFile is not null)
  {
    vehicle.Image = await UserAssetStorage.SaveImageAsync(form.VehicleImageFile, "vehicles", vehicle.Id, "vehicle-image", environment);
  }

  if (form.DriverImageFile is not null)
  {
    vehicle.DriverImage = await UserAssetStorage.SaveImageAsync(form.DriverImageFile, "vehicles", vehicle.Id, "driver-image", environment);
  }

  vehicle.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, request, "vehicles", "Create", vehicle.Id, $"Created vehicle {vehicle.Id}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToVehicleDto(vehicle, request));
}).DisableAntiforgery();

app.MapPut("/api/vehicles/{vehicleId}", async (
  string vehicleId,
  [FromForm] VehicleFormData form,
  HttpRequest request,
  IWebHostEnvironment environment,
  FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(request, db, "vehicles", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateVehicleRequest(form);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var vehicle = await db.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId && item.IsDeleted == 0);
  if (vehicle is null) return Results.NotFound(new ApiError("Vehicle not found."));

  var normalizedPlate = form.Plate!.Trim().ToUpperInvariant();
  var duplicatePlate = await db.Vehicles.AnyAsync(item =>
    item.Id != vehicleId &&
    item.IsDeleted == 0 &&
    item.Plate.ToLower() == normalizedPlate.ToLower());
  if (duplicatePlate) return Results.BadRequest(new ApiError("Vehicle plate already exists."));

  vehicle.Plate = normalizedPlate;
  vehicle.Region = form.Region!.Trim();
  vehicle.Type = form.Type!.Trim();
  vehicle.Model = form.Model!.Trim();
  vehicle.Make = NormalizeOptional(form.Make);
  vehicle.Year = NormalizeOptional(form.Year);
  vehicle.Color = NormalizeOptional(form.Color);
  vehicle.Status = form.Status!.Trim();
  vehicle.Ownership = NormalizeOptional(form.Ownership) ?? "Owned";
  vehicle.Driver = form.Driver!.Trim();
  vehicle.Depot = NormalizeOptional(form.Depot);
  vehicle.Capacity = NormalizeOptional(form.Capacity);
  vehicle.FuelCapacity = NormalizeOptional(form.FuelCapacity);
  vehicle.FuelType = form.FuelType!.Trim();
  vehicle.Vin = NormalizeOptional(form.Vin);
  vehicle.EngineNo = NormalizeOptional(form.EngineNo);
  vehicle.Odometer = NormalizeOptional(form.Odometer);
  vehicle.LastService = NormalizeOptional(form.LastService);
  vehicle.NextService = NormalizeOptional(form.NextService);
  vehicle.ServiceNote = NormalizeOptional(form.ServiceNote);
  vehicle.PurchaseCost = NormalizeOptional(form.PurchaseCost);
  vehicle.RegistrationNo = NormalizeOptional(form.RegistrationNo);
  vehicle.RegistrationExpiry = NormalizeOptional(form.RegistrationExpiry);
  vehicle.RoadTaxExpiry = NormalizeOptional(form.RoadTaxExpiry);
  vehicle.InsuranceExpiry = NormalizeOptional(form.InsuranceExpiry);
  vehicle.InsuranceProvider = NormalizeOptional(form.InsuranceProvider);
  vehicle.InsurancePolicy = NormalizeOptional(form.InsurancePolicy);
  vehicle.InspectionDue = NormalizeOptional(form.InspectionDue);
  vehicle.AcquiredDate = NormalizeOptional(form.AcquiredDate);

  if (form.RemoveVehicleImage)
  {
    vehicle.Image = string.Empty;
  }

  if (form.RemoveDriverImage)
  {
    vehicle.DriverImage = string.Empty;
  }

  if (form.DriverImageFile is null && !form.RemoveDriverImage)
  {
    var driverAvatar = await db.Users
      .Include(user => user.Role)
      .Where(user =>
        user.IsDeleted == 0 &&
        user.Status == "Active" &&
        user.Name == vehicle.Driver &&
        user.Role != null &&
        user.Role.Name == "Driver")
      .Select(user => user.Avatar)
      .FirstOrDefaultAsync();

    vehicle.DriverImage = NormalizeOptional(driverAvatar);
  }

  if (form.VehicleImageFile is not null)
  {
    vehicle.Image = await UserAssetStorage.SaveImageAsync(form.VehicleImageFile, "vehicles", vehicle.Id, "vehicle-image", environment);
  }

  if (form.DriverImageFile is not null)
  {
    vehicle.DriverImage = await UserAssetStorage.SaveImageAsync(form.DriverImageFile, "vehicles", vehicle.Id, "driver-image", environment);
  }

  vehicle.UpdatedAt = DateTime.UtcNow;

  await LogAuditAsync(db, request, "vehicles", "Edit", vehicle.Id, $"Updated vehicle {vehicle.Id}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToVehicleDto(vehicle, request));
}).DisableAntiforgery();

app.MapPatch("/api/vehicles/{vehicleId}/status", async (string vehicleId, VehicleStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "vehicles", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
  if (string.IsNullOrWhiteSpace(normalizedStatus))
  {
    return Results.BadRequest(new ApiError("Vehicle status is required."));
  }

  var vehicle = await db.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId && item.IsDeleted == 0);
  if (vehicle is null) return Results.NotFound(new ApiError("Vehicle not found."));

  var oldStatus = vehicle.Status;
  vehicle.Status = normalizedStatus;
  vehicle.UpdatedAt = DateTime.UtcNow;
  AddStatusHistoryIfChanged(db, httpRequest, "Vehicle", vehicle.Id, oldStatus, vehicle.Status);
  await LogAuditAsync(db, httpRequest, "vehicles", "Edit", vehicle.Id, $"Changed vehicle status for {vehicle.Id}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToVehicleDto(vehicle, httpRequest));
});

app.MapDelete("/api/vehicles/{vehicleId}", async (string vehicleId, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "vehicles", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var vehicle = await db.Vehicles.FirstOrDefaultAsync(item => item.Id == vehicleId && item.IsDeleted == 0);
  if (vehicle is null) return Results.NotFound(new ApiError("Vehicle not found."));

  vehicle.IsDeleted = 1;
  vehicle.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "vehicles", "Delete", vehicle.Id, $"Deleted vehicle {vehicle.Id}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/incidents", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? status = null,
  string? severity = null,
  string? sortBy = "date",
  string? sortOrder = "desc") =>
{
  var query = db.Incidents
    .Where(incident => incident.IsDeleted == 0)
    .AsNoTracking()
    .AsQueryable();

  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(incident =>
      incident.Id.ToLower().Contains(normalizedSearch) ||
      incident.VehicleId.ToLower().Contains(normalizedSearch) ||
      incident.Driver.ToLower().Contains(normalizedSearch) ||
      incident.Type.ToLower().Contains(normalizedSearch) ||
      (incident.Notes ?? string.Empty).ToLower().Contains(normalizedSearch));
  }

  if (!string.IsNullOrWhiteSpace(status) && status != "All")
  {
    var normalizedStatus = status.Trim().ToLower();
    query = query.Where(incident => incident.Status.ToLower() == normalizedStatus);
  }

  if (!string.IsNullOrWhiteSpace(severity) && severity != "All")
  {
    var normalizedSeverity = severity.Trim().ToLower();
    query = query.Where(incident => incident.Severity.ToLower() == normalizedSeverity);
  }

  query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
  {
    ("id", "asc") => query.OrderBy(incident => incident.Id),
    ("id", _) => query.OrderByDescending(incident => incident.Id),
    ("status", "asc") => query.OrderBy(incident => incident.Status),
    ("status", _) => query.OrderByDescending(incident => incident.Status),
    ("severity", "asc") => query.OrderBy(incident => incident.Severity),
    ("severity", _) => query.OrderByDescending(incident => incident.Severity),
    ("date", "asc") => query.OrderBy(incident => incident.Date),
    _ => query.OrderByDescending(incident => incident.Date)
  };

  var total = await query.CountAsync();
  var records = await query
    .Skip(Math.Max(page - 1, 0) * pageSize)
    .Take(pageSize)
    .ToListAsync();

  return Results.Ok(new PagedResult<IncidentDto>(records.Select(ToIncidentDto).ToList(), total));
});

app.MapPost("/api/incidents", async (IncidentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "incidents", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateIncidentRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var now = DateTime.UtcNow;
  var incident = new Incident
  {
    Id = NextIncidentId(await db.Incidents.Select(item => item.Id).ToListAsync()),
    VehicleId = request.VehicleId.Trim(),
    Driver = request.Driver.Trim(),
    Date = request.Date.Trim(),
    Type = request.Type.Trim(),
    Severity = request.Severity.Trim(),
    Status = request.Status.Trim(),
    Cost = NormalizeOptional(request.Cost),
    Notes = NormalizeOptional(request.Notes),
    IsDeleted = 0,
    CreatedAt = now,
    UpdatedAt = now
  };

  db.Incidents.Add(incident);
  await db.SaveChangesAsync();
  return Results.Ok(ToIncidentDto(incident));
});

app.MapPut("/api/incidents/{incidentId}", async (string incidentId, IncidentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "incidents", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateIncidentRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var incident = await db.Incidents.FirstOrDefaultAsync(item => item.Id == incidentId && item.IsDeleted == 0);
  if (incident is null) return Results.NotFound(new ApiError("Incident not found."));

  var oldStatus = incident.Status;
  incident.VehicleId = request.VehicleId.Trim();
  incident.Driver = request.Driver.Trim();
  incident.Date = request.Date.Trim();
  incident.Type = request.Type.Trim();
  incident.Severity = request.Severity.Trim();
  incident.Status = request.Status.Trim();
  incident.Cost = NormalizeOptional(request.Cost);
  incident.Notes = NormalizeOptional(request.Notes);
  incident.UpdatedAt = DateTime.UtcNow;

  AddStatusHistoryIfChanged(db, httpRequest, "Incident", incident.Id, oldStatus, incident.Status);
  await LogAuditAsync(db, httpRequest, "incidents", "Edit", incident.Id, $"Updated incident {incident.Id}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToIncidentDto(incident));
});

app.MapDelete("/api/incidents/{incidentId}", async (string incidentId, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "incidents", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var incident = await db.Incidents.FirstOrDefaultAsync(item => item.Id == incidentId && item.IsDeleted == 0);
  if (incident is null) return Results.NotFound(new ApiError("Incident not found."));

  incident.IsDeleted = 1;
  incident.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "incidents", "Delete", incident.Id, $"Deleted incident {incident.Id}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/expenses", async (
  HttpRequest httpRequest,
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? status = null,
  string? dateFrom = null,
  string? dateTo = null) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.View);
  if (permissionError is not null) return permissionError;

  var query = db.Expenses.Where(expense => expense.IsDeleted == 0).AsNoTracking().AsQueryable();
  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(expense =>
      expense.ExpenseType.ToLower().Contains(normalizedSearch) ||
      (expense.VehicleId ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      (expense.TripNumber ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      (expense.DriverName ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      (expense.Notes ?? string.Empty).ToLower().Contains(normalizedSearch));
  }
  if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(expense => expense.Status == status);
  if (!string.IsNullOrWhiteSpace(dateFrom)) query = query.Where(expense => string.Compare(expense.ExpenseDate, dateFrom) >= 0);
  if (!string.IsNullOrWhiteSpace(dateTo)) query = query.Where(expense => string.Compare(expense.ExpenseDate, dateTo) <= 0);

  var total = await query.CountAsync();
  var records = await query.OrderByDescending(expense => expense.ExpenseDate).Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
  return Results.Ok(new PagedResult<ExpenseDto>(records.Select(ToExpenseDto).ToList(), total));
});

app.MapPost("/api/expenses", async (ExpenseRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.Create);
  if (permissionError is not null) return permissionError;
  var validationError = ValidateExpenseRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var now = DateTime.UtcNow;
  var expense = new Expense
  {
    ExpenseDate = request.ExpenseDate.Trim(),
    ExpenseType = request.ExpenseType.Trim(),
    VehicleId = NormalizeOptional(request.VehicleId),
    TripNumber = NormalizeOptional(request.TripNumber),
    DriverName = NormalizeOptional(request.DriverName),
    Amount = request.Amount,
    Status = request.Status.Trim(),
    Notes = NormalizeOptional(request.Notes),
    CreatedAt = now,
    UpdatedAt = now
  };
  db.Expenses.Add(expense);
  await db.SaveChangesAsync();
  await LogAuditAsync(db, httpRequest, "expenses", "Create", expense.Id.ToString(), $"Created expense {expense.ExpenseType}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToExpenseDto(expense));
});

app.MapPut("/api/expenses/{id:int}", async (int id, ExpenseRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;
  var validationError = ValidateExpenseRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));

  var expense = await db.Expenses.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
  if (expense is null) return Results.NotFound(new ApiError("Expense not found."));
  var oldStatus = expense.Status;
  expense.ExpenseDate = request.ExpenseDate.Trim();
  expense.ExpenseType = request.ExpenseType.Trim();
  expense.VehicleId = NormalizeOptional(request.VehicleId);
  expense.TripNumber = NormalizeOptional(request.TripNumber);
  expense.DriverName = NormalizeOptional(request.DriverName);
  expense.Amount = request.Amount;
  expense.Status = request.Status.Trim();
  expense.Notes = NormalizeOptional(request.Notes);
  expense.UpdatedAt = DateTime.UtcNow;
  AddStatusHistoryIfChanged(db, httpRequest, "Expense", expense.Id.ToString(), oldStatus, expense.Status);
  await LogAuditAsync(db, httpRequest, "expenses", "Edit", expense.Id.ToString(), $"Updated expense {expense.ExpenseType}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToExpenseDto(expense));
});

app.MapDelete("/api/expenses/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "expenses", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;
  var expense = await db.Expenses.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
  if (expense is null) return Results.NotFound(new ApiError("Expense not found."));
  expense.IsDeleted = 1;
  expense.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "expenses", "Delete", expense.Id.ToString(), $"Deleted expense {expense.ExpenseType}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

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
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.View);
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
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Create);
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
  await LogAuditAsync(db, httpRequest, moduleKey, "Create", document.Id.ToString(), $"Created {document.OwnerType} document {document.DocumentType}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToFleetDocumentDto(document));
});

app.MapPut("/api/documents/{id:int}", async (int id, FleetDocumentRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var moduleKey = request.OwnerType == "Driver" ? "driver-documents" : "vehicle-documents";
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Edit);
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
  AddStatusHistoryIfChanged(db, httpRequest, "Document", document.Id.ToString(), oldStatus, document.Status);
  await LogAuditAsync(db, httpRequest, moduleKey, "Edit", document.Id.ToString(), $"Updated {document.OwnerType} document {document.DocumentType}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToFleetDocumentDto(document));
});

app.MapDelete("/api/documents/{id:int}", async (int id, string ownerType, HttpRequest httpRequest, FleetDbContext db) =>
{
  var moduleKey = ownerType == "Driver" ? "driver-documents" : "vehicle-documents";
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Delete);
  if (permissionError is not null) return permissionError;
  var document = await db.FleetDocuments.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
  if (document is null) return Results.NotFound(new ApiError("Document not found."));
  document.IsDeleted = 1;
  document.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, moduleKey, "Delete", document.Id.ToString(), $"Deleted {document.OwnerType} document {document.DocumentType}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/audit-logs", async (HttpRequest httpRequest, FleetDbContext db, string? module = null, int page = 1, int pageSize = 20) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "audit-logs", PermissionAction.View);
  if (permissionError is not null) return permissionError;

  var query = db.AuditLogs.AsNoTracking().AsQueryable();
  if (!string.IsNullOrWhiteSpace(module) && module != "All") query = query.Where(log => log.ModuleKey == module);
  var total = await query.CountAsync();
  var records = await query.OrderByDescending(log => log.CreatedAt).Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
  return Results.Ok(new PagedResult<AuditLogDto>(records.Select(ToAuditLogDto).ToList(), total));
});

app.MapGet("/api/status-history", async (HttpRequest httpRequest, FleetDbContext db, string? entityType = null, string? entityId = null, int page = 1, int pageSize = 20) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "audit-logs", PermissionAction.View);
  if (permissionError is not null) return permissionError;

  var query = db.StatusHistories.AsNoTracking().AsQueryable();
  if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(history => history.EntityType == entityType);
  if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(history => history.EntityId == entityId);
  var total = await query.CountAsync();
  var records = await query.OrderByDescending(history => history.CreatedAt).Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
  return Results.Ok(new PagedResult<StatusHistoryDto>(records.Select(ToStatusHistoryDto).ToList(), total));
});

app.MapGet("/api/reports/{reportType}", async (
  string reportType,
  HttpRequest httpRequest,
  FleetDbContext db,
  string? dateFrom = null,
  string? dateTo = null,
  string? status = null,
  string? vehicleId = null,
  string? driver = null) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "reports", PermissionAction.View);
  if (permissionError is not null) return permissionError;

  DateTime? parsedDateFrom = DateTime.TryParse(dateFrom, out var startDate) ? startDate.Date : null;
  DateTime? parsedDateTo = DateTime.TryParse(dateTo, out var endDate) ? endDate.Date.AddDays(1).AddTicks(-1) : null;

  object rows = reportType.ToLowerInvariant() switch
  {
    "vehicles" => await db.Vehicles.AsNoTracking()
      .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (parsedDateFrom == null || item.CreatedAt >= parsedDateFrom) && (parsedDateTo == null || item.CreatedAt <= parsedDateTo))
      .Select(item => new { item.Id, item.Plate, item.Type, item.Status, item.Driver, item.Depot })
      .ToListAsync(),
    "trips" => await db.Trips.AsNoTracking()
      .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (string.IsNullOrWhiteSpace(vehicleId) || item.VehicleId == vehicleId) && (string.IsNullOrWhiteSpace(driver) || item.DriverName == driver) && (string.IsNullOrWhiteSpace(dateFrom) || string.Compare(item.DepartureDateTime, dateFrom) >= 0) && (string.IsNullOrWhiteSpace(dateTo) || string.Compare(item.DepartureDateTime, dateTo) <= 0))
      .Select(item => new { item.TripNumber, item.VehicleId, item.DriverName, item.Status, item.PickupLocation, item.DropoffLocation })
      .ToListAsync(),
    "maintenance" => await db.MaintenanceTickets.AsNoTracking()
      .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (string.IsNullOrWhiteSpace(vehicleId) || item.VehicleId == vehicleId) && (string.IsNullOrWhiteSpace(dateFrom) || string.Compare(item.ReportedDate, dateFrom) >= 0) && (string.IsNullOrWhiteSpace(dateTo) || string.Compare(item.ReportedDate, dateTo) <= 0))
      .Select(item => new { item.Id, item.VehicleId, item.Issue, item.Mechanic, item.Status, item.ReportedDate })
      .ToListAsync(),
    "drivers" => await db.Users.AsNoTracking().Include(item => item.Role)
      .Where(item => item.IsDeleted == 0 && item.Role != null && item.Role.Name == "Driver" && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (parsedDateFrom == null || item.CreatedAt >= parsedDateFrom) && (parsedDateTo == null || item.CreatedAt <= parsedDateTo))
      .Select(item => new { item.EmployeeId, item.Name, item.Email, item.Phone, item.Status, item.LicenseExpiry })
      .ToListAsync(),
    "expenses" => await db.Expenses.AsNoTracking()
      .Where(item => item.IsDeleted == 0 && (string.IsNullOrWhiteSpace(status) || item.Status == status) && (string.IsNullOrWhiteSpace(vehicleId) || item.VehicleId == vehicleId) && (string.IsNullOrWhiteSpace(driver) || item.DriverName == driver) && (string.IsNullOrWhiteSpace(dateFrom) || string.Compare(item.ExpenseDate, dateFrom) >= 0) && (string.IsNullOrWhiteSpace(dateTo) || string.Compare(item.ExpenseDate, dateTo) <= 0))
      .Select(item => new { item.ExpenseDate, item.ExpenseType, item.VehicleId, item.TripNumber, item.DriverName, item.Amount, item.Status })
      .ToListAsync(),
    _ => Array.Empty<object>()
  };
  return Results.Ok(rows);
});

app.MapGet("/api/trips", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? status = null,
  string? tripType = null,
  string? sortBy = "id",
  string? sortOrder = "desc") =>
{
  var query = db.Trips.Where(trip => trip.IsDeleted == 0).AsNoTracking().AsQueryable();
  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(trip =>
      trip.TripNumber.ToLower().Contains(normalizedSearch) ||
      trip.PickupLocation.ToLower().Contains(normalizedSearch) ||
      trip.DropoffLocation.ToLower().Contains(normalizedSearch) ||
      trip.DriverName.ToLower().Contains(normalizedSearch) ||
      (trip.CoDriverName ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      trip.DispatcherName.ToLower().Contains(normalizedSearch) ||
      trip.CustomerName.ToLower().Contains(normalizedSearch) ||
      trip.VehicleId.ToLower().Contains(normalizedSearch) ||
      trip.VehiclePlate.ToLower().Contains(normalizedSearch) ||
      trip.CargoType.ToLower().Contains(normalizedSearch));
  }

  if (!string.IsNullOrWhiteSpace(status) && status != "All")
  {
    var normalizedStatus = status.Trim().ToLower();
    query = query.Where(trip => trip.Status.ToLower() == normalizedStatus);
  }

  if (!string.IsNullOrWhiteSpace(tripType) && tripType != "All")
  {
    var normalizedType = tripType.Trim().ToLower();
    query = query.Where(trip => trip.TripType.ToLower() == normalizedType);
  }

  query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
  {
    ("tripnumber", "asc") => query.OrderBy(trip => trip.TripNumber),
    ("tripnumber", _) => query.OrderByDescending(trip => trip.TripNumber),
    ("status", "asc") => query.OrderBy(trip => trip.Status),
    ("status", _) => query.OrderByDescending(trip => trip.Status),
    ("triptype", "asc") => query.OrderBy(trip => trip.TripType),
    ("triptype", _) => query.OrderByDescending(trip => trip.TripType),
    ("departure", "asc") => query.OrderBy(trip => trip.DepartureDateTime),
    ("departure", _) => query.OrderByDescending(trip => trip.DepartureDateTime),
    ("id", "asc") => query.OrderBy(trip => trip.Id),
    _ => query.OrderByDescending(trip => trip.Id)
  };

  var total = await query.CountAsync();
  var records = await query.Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
  return Results.Ok(new PagedResult<TripDto>(records.Select(ToTripDto).ToList(), total));
});

app.MapPost("/api/trips", async (TripRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateTripRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
  var duplicate = await db.Trips.AnyAsync(trip => trip.IsDeleted == 0 && trip.TripNumber.ToLower() == request.TripNumber!.Trim().ToLower());
  if (duplicate) return Results.BadRequest(new ApiError("Trip number already exists."));

  var now = DateTime.UtcNow;
  var trip = ApplyTripRequest(new Trip { CreatedAt = now, IsDeleted = 0 }, request);
  trip.UpdatedAt = now;
  db.Trips.Add(trip);
  await LogAuditAsync(db, httpRequest, "trips", "Create", request.TripNumber!.Trim(), $"Created trip {request.TripNumber!.Trim()}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToTripDto(trip));
});

app.MapPut("/api/trips/{id:int}", async (int id, TripRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateTripRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
  var trip = await db.Trips.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
  if (trip is null) return Results.NotFound(new ApiError("Trip not found."));
  var duplicate = await db.Trips.AnyAsync(item => item.Id != id && item.IsDeleted == 0 && item.TripNumber.ToLower() == request.TripNumber!.Trim().ToLower());
  if (duplicate) return Results.BadRequest(new ApiError("Trip number already exists."));

  var oldStatus = trip.Status;
  ApplyTripRequest(trip, request);
  trip.UpdatedAt = DateTime.UtcNow;
  AddStatusHistoryIfChanged(db, httpRequest, "Trip", trip.Id.ToString(), oldStatus, trip.Status);
  await LogAuditAsync(db, httpRequest, "trips", "Edit", trip.Id.ToString(), $"Updated trip {trip.TripNumber}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToTripDto(trip));
});

app.MapDelete("/api/trips/{id:int}", async (int id, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "trips", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var trip = await db.Trips.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == 0);
  if (trip is null) return Results.NotFound(new ApiError("Trip not found."));
  trip.IsDeleted = 1;
  trip.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "trips", "Delete", trip.Id.ToString(), $"Deleted trip {trip.TripNumber}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/inventory-parts", async (
  HttpRequest httpRequest,
  FleetDbContext db,
  string? search = null,
  string? category = null,
  string? stock = null) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.View);
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
  var permissionError = await RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.Create);
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
  await LogAuditAsync(db, httpRequest, "inventory-parts", "Create", part.Id, $"Created inventory part {part.Name}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToInventoryPartDto(part, httpRequest));
}).DisableAntiforgery();

app.MapPut("/api/inventory-parts/{partId}", async (string partId, [FromForm] InventoryPartForm form, HttpRequest httpRequest, FleetDbContext db, IWebHostEnvironment environment) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.Edit);
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
  if (form.RemoveImage) part.Image = null;
  if (form.ImageFile is not null)
  {
    part.Image = await UserAssetStorage.SaveImageAsync(form.ImageFile, "inventory-parts", part.Id, "part-image", environment);
  }
  part.UpdatedAt = DateTime.UtcNow;

  await LogAuditAsync(db, httpRequest, "inventory-parts", "Edit", part.Id, $"Updated inventory part {part.Name}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToInventoryPartDto(part, httpRequest));
}).DisableAntiforgery();

app.MapDelete("/api/inventory-parts/{partId}", async (string partId, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "inventory-parts", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var part = await db.InventoryParts.FirstOrDefaultAsync(item => item.Id == partId && item.IsDeleted == 0);
  if (part is null) return Results.NotFound(new ApiError("Inventory part not found."));

  part.IsDeleted = 1;
  part.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "inventory-parts", "Delete", part.Id, $"Deleted inventory part {part.Name}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.MapGet("/api/maintenance-tickets", async (
  FleetDbContext db,
  int page = 1,
  int pageSize = 10,
  string? search = null,
  string? status = null,
  string? sortBy = "id",
  string? sortOrder = "asc") =>
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
    _ => query.OrderBy(ticket => ticket.Id)
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

app.MapPost("/api/maintenance-tickets", async (MaintenanceTicketRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Create);
  if (permissionError is not null) return permissionError;

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
  await LogAuditAsync(db, httpRequest, "maintenance-tickets", "Create", ticket.Id, $"Created maintenance ticket {ticket.Id}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToMaintenanceTicketDto(ticket));
});

app.MapPut("/api/maintenance-tickets/{ticketId}", async (string ticketId, MaintenanceTicketRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

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

  await LogAuditAsync(db, httpRequest, "maintenance-tickets", "Edit", ticket.Id, $"Updated maintenance ticket {ticket.Id}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToMaintenanceTicketDto(ticket));
});

app.MapPatch("/api/maintenance-tickets/{ticketId}/status", async (string ticketId, MaintenanceTicketStatusRequest request, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
  if (string.IsNullOrWhiteSpace(normalizedStatus))
  {
    return Results.BadRequest(new ApiError("Ticket status is required."));
  }

  var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
  if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

  var oldStatus = ticket.Status;
  ticket.Status = normalizedStatus;
  ticket.UpdatedAt = DateTime.UtcNow;
  AddStatusHistoryIfChanged(db, httpRequest, "MaintenanceTicket", ticket.Id, oldStatus, ticket.Status);
  await LogAuditAsync(db, httpRequest, "maintenance-tickets", "Edit", ticket.Id, $"Changed maintenance ticket status {ticket.Id}.");
  await db.SaveChangesAsync();

  return Results.Ok(ToMaintenanceTicketDto(ticket));
});

app.MapDelete("/api/maintenance-tickets/{ticketId}", async (string ticketId, HttpRequest httpRequest, FleetDbContext db) =>
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, "maintenance-tickets", PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var ticket = await db.MaintenanceTickets.FirstOrDefaultAsync(item => item.Id == ticketId && item.IsDeleted == 0);
  if (ticket is null) return Results.NotFound(new ApiError("Ticket not found."));

  ticket.IsDeleted = 1;
  ticket.UpdatedAt = DateTime.UtcNow;
  await LogAuditAsync(db, httpRequest, "maintenance-tickets", "Delete", ticket.Id, $"Deleted maintenance ticket {ticket.Id}.");
  await db.SaveChangesAsync();
  return Results.NoContent();
});

app.Run();

static async Task<IResult?> RequirePermissionAsync(
  HttpRequest request,
  FleetDbContext db,
  string moduleKey,
  PermissionAction action)
{
  var roleId = request.Headers["X-Fleet-Role-Id"].FirstOrDefault();
  if (string.IsNullOrWhiteSpace(roleId))
  {
    return Results.Json(new ApiError("Login session is required."), statusCode: StatusCodes.Status401Unauthorized);
  }

  var savedPermission = await db.RolePermissions
    .AsNoTracking()
    .FirstOrDefaultAsync(permission =>
      permission.RoleId == roleId &&
      permission.ModuleKey == moduleKey);

  var defaultPermission = GetDefaultPermission(roleId, moduleKey);
  var allowed = action switch
  {
    PermissionAction.View => savedPermission?.CanView ?? defaultPermission.CanView,
    PermissionAction.Create => savedPermission?.CanCreate ?? defaultPermission.CanCreate,
    PermissionAction.Edit => savedPermission?.CanEdit ?? defaultPermission.CanEdit,
    PermissionAction.Delete => savedPermission?.CanDelete ?? defaultPermission.CanDelete,
    _ => false
  };

  return allowed
    ? null
    : Results.Json(new ApiError("You do not have permission to perform this action."), statusCode: StatusCodes.Status403Forbidden);
}

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

static LocationTypeDto ToLocationTypeDto(LocationTypeCodeOption locationType) =>
  new(
    locationType.Id,
    locationType.Name,
    locationType.Code,
    locationType.Description,
    locationType.Status,
    locationType.CreatedAt,
    locationType.UpdatedAt);

static VehicleTypeDto ToVehicleTypeDto(VehicleTypeCodeOption vehicleType) =>
  new(
    vehicleType.Id,
    vehicleType.Name,
    vehicleType.Code,
    vehicleType.Description,
    vehicleType.Status,
    vehicleType.CreatedAt,
    vehicleType.UpdatedAt);

static FuelTypeDto ToFuelTypeDto(FuelTypeCodeOption fuelType) =>
  new(
    fuelType.Id,
    fuelType.Name,
    fuelType.Code,
    fuelType.Description,
    fuelType.Status,
    fuelType.CreatedAt,
    fuelType.UpdatedAt);

static VehicleDto ToVehicleDto(Vehicle vehicle, HttpRequest request) =>
  new(
    vehicle.Id,
    vehicle.Plate,
    vehicle.Region,
    vehicle.Type,
    vehicle.Model,
    vehicle.Make,
    vehicle.Year,
    vehicle.Color,
    vehicle.Status,
    vehicle.Ownership,
    vehicle.Driver,
    ToPublicAssetUrl(request, vehicle.DriverImage),
    vehicle.Depot,
    vehicle.Capacity,
    vehicle.FuelCapacity,
    vehicle.FuelType,
    vehicle.Vin,
    vehicle.EngineNo,
    vehicle.Odometer,
    vehicle.LastService,
    vehicle.NextService,
    vehicle.ServiceNote,
    vehicle.PurchaseCost,
    vehicle.RegistrationNo,
    vehicle.RegistrationExpiry,
    vehicle.RoadTaxExpiry,
    vehicle.InsuranceExpiry,
    vehicle.InsuranceProvider,
    vehicle.InsurancePolicy,
    vehicle.InspectionDue,
    vehicle.AcquiredDate,
    ToPublicAssetUrl(request, vehicle.Image),
    vehicle.CreatedAt,
    vehicle.UpdatedAt);

static TripDto ToTripDto(Trip trip) =>
  new(
    trip.Id,
    trip.TripNumber,
    trip.TripType,
    trip.Status,
    trip.Priority,
    trip.CustomerName,
    trip.Department,
    trip.CostCenter,
    trip.VehicleId,
    trip.VehiclePlate,
    trip.TrailerNumber,
    trip.DriverName,
    trip.CoDriverName,
    trip.DispatcherName,
    trip.CargoType,
    trip.LoadWeightKg,
    trip.LoadVolumeM3,
    trip.PickupLocation,
    trip.DropoffLocation,
    trip.PickupContact,
    trip.DropoffContact,
    trip.DepartureDateTime,
    trip.EstimatedArrival,
    trip.ActualArrival,
    trip.PlannedDistanceKm,
    trip.StartingOdometerKm,
    trip.CurrentOdometerKm,
    trip.EndingOdometerKm,
    trip.FuelIssuedLiters,
    trip.TollEstimate,
    trip.PermitRequired,
    trip.TemperatureControlled,
    trip.TemperatureRange,
    trip.SpecialInstructions,
    trip.DriverNotes,
    trip.CreatedAt,
    trip.UpdatedAt);

static TripSetupDto ToTripSetupDto(TripSetupCodeOption option) =>
  new(option.Id, option.Name, option.Code, option.Description, option.Status, option.CreatedAt, option.UpdatedAt);

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

static InventoryPartDto ToInventoryPartDto(InventoryPart part, HttpRequest request) =>
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
    ToPublicAssetUrl(request, part.Image),
    part.CreatedAt,
    part.UpdatedAt);

static IncidentDto ToIncidentDto(Incident incident) =>
  new(
    incident.Id,
    incident.VehicleId,
    incident.Driver,
    incident.Date,
    incident.Type,
    incident.Severity,
    incident.Status,
    incident.Cost,
    incident.Notes,
    incident.CreatedAt,
    incident.UpdatedAt);

static ExpenseDto ToExpenseDto(Expense expense) =>
  new(
    expense.Id,
    expense.ExpenseDate,
    expense.ExpenseType,
    expense.VehicleId,
    expense.TripNumber,
    expense.DriverName,
    expense.Amount,
    expense.Status,
    expense.Notes,
    expense.CreatedAt,
    expense.UpdatedAt);

static FleetDocumentDto ToFleetDocumentDto(FleetDocument document) =>
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

static AuditLogDto ToAuditLogDto(AuditLog log) =>
  new(log.Id, log.RoleId, log.ModuleKey, log.Action, log.EntityId, log.Description, log.CreatedAt);

static StatusHistoryDto ToStatusHistoryDto(StatusHistory history) =>
  new(history.Id, history.EntityType, history.EntityId, history.OldStatus, history.NewStatus, history.RoleId, history.CreatedAt);

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

static string NextInventoryPartId(IEnumerable<string> existingIds)
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

static string NextIncidentId(IEnumerable<string> existingIds)
{
  var max = existingIds
    .Select(value =>
    {
      if (string.IsNullOrWhiteSpace(value)) return 0;
      var normalized = value.StartsWith("INC-", StringComparison.OrdinalIgnoreCase)
        ? value[4..]
        : value;
      return int.TryParse(normalized, out var number) ? number : 0;
    })
    .DefaultIfEmpty(1000)
    .Max();

  return $"INC-{max + 1}";
}

static string NextVehicleId(IEnumerable<string> existingIds)
{
  var max = existingIds
    .Select(value =>
    {
      if (string.IsNullOrWhiteSpace(value)) return 0;
      var normalized = value.StartsWith("VH-", StringComparison.OrdinalIgnoreCase)
        ? value[3..]
        : value;
      return int.TryParse(normalized, out var number) ? number : 0;
    })
    .DefaultIfEmpty(1000)
    .Max();

  return $"VH-{max + 1:D4}";
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

static string? ValidateVehicleTypeRequest(VehicleTypeRequest request)
{
  if (string.IsNullOrWhiteSpace(request.Name)) return "Vehicle type name is required.";
  if (request.Name.Trim().Length > 120) return "Vehicle type name must be 120 characters or fewer.";
  if (string.IsNullOrWhiteSpace(request.Code)) return "Vehicle type code is required.";
  if (request.Code.Trim().Length > 40) return "Vehicle type code must be 40 characters or fewer.";
  if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
  {
    return "Vehicle type description must be 500 characters or fewer.";
  }

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  return normalizedStatus is "Active" or "Disabled"
    ? null
    : "Vehicle type status must be Active or Disabled.";
}

static string? ValidateLocationTypeRequest(LocationTypeRequest request)
{
  if (string.IsNullOrWhiteSpace(request.Name)) return "Location type name is required.";
  if (request.Name.Trim().Length > 120) return "Location type name must be 120 characters or fewer.";
  if (string.IsNullOrWhiteSpace(request.Code)) return "Location type code is required.";
  if (request.Code.Trim().Length > 40) return "Location type code must be 40 characters or fewer.";
  if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
  {
    return "Location type description must be 500 characters or fewer.";
  }

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  return normalizedStatus is "Active" or "Disabled"
    ? null
    : "Location type status must be Active or Disabled.";
}

static string? ValidateFuelTypeRequest(FuelTypeRequest request)
{
  if (string.IsNullOrWhiteSpace(request.Name)) return "Fuel type name is required.";
  if (request.Name.Trim().Length > 120) return "Fuel type name must be 120 characters or fewer.";
  if (string.IsNullOrWhiteSpace(request.Code)) return "Fuel type code is required.";
  if (request.Code.Trim().Length > 40) return "Fuel type code must be 40 characters or fewer.";
  if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500)
  {
    return "Fuel type description must be 500 characters or fewer.";
  }

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  return normalizedStatus is "Active" or "Disabled"
    ? null
    : "Fuel type status must be Active or Disabled.";
}

static string? ValidateVehicleRequest(VehicleFormData request)
{
  if (string.IsNullOrWhiteSpace(request.Plate)) return "Plate number is required.";
  if (request.Plate.Trim().Length > 40) return "Plate number must be 40 characters or fewer.";
  if (string.IsNullOrWhiteSpace(request.Region)) return "Region is required.";
  if (string.IsNullOrWhiteSpace(request.Type)) return "Vehicle type is required.";
  if (string.IsNullOrWhiteSpace(request.Model)) return "Vehicle model is required.";
  if (string.IsNullOrWhiteSpace(request.Driver)) return "Driver is required.";
  if (string.IsNullOrWhiteSpace(request.FuelType)) return "Fuel type is required.";

  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? string.Empty : request.Status.Trim();
  return string.IsNullOrWhiteSpace(normalizedStatus) ? "Vehicle status is required." : null;
}

static string? ValidateTripRequest(TripRequest request)
{
  if (string.IsNullOrWhiteSpace(request.TripNumber)) return "Trip number is required.";
  if (string.IsNullOrWhiteSpace(request.TripType)) return "Trip type is required.";
  if (string.IsNullOrWhiteSpace(request.Status)) return "Trip status is required.";
  if (string.IsNullOrWhiteSpace(request.Priority)) return "Priority is required.";
  if (string.IsNullOrWhiteSpace(request.CustomerName)) return "Customer is required.";
  if (string.IsNullOrWhiteSpace(request.Department)) return "Department is required.";
  if (string.IsNullOrWhiteSpace(request.VehicleId)) return "Vehicle is required.";
  if (string.IsNullOrWhiteSpace(request.VehiclePlate)) return "Vehicle plate is required.";
  if (string.IsNullOrWhiteSpace(request.DriverName)) return "Driver is required.";
  if (string.IsNullOrWhiteSpace(request.DispatcherName)) return "Dispatcher is required.";
  if (string.IsNullOrWhiteSpace(request.CargoType)) return "Cargo type is required.";
  if (string.IsNullOrWhiteSpace(request.PickupLocation)) return "Pickup location is required.";
  if (string.IsNullOrWhiteSpace(request.DropoffLocation)) return "Dropoff location is required.";
  if (string.IsNullOrWhiteSpace(request.DepartureDateTime)) return "Departure date and time is required.";
  if (string.IsNullOrWhiteSpace(request.EstimatedArrival)) return "Estimated arrival is required.";
  return null;
}

static Trip ApplyTripRequest(Trip trip, TripRequest request)
{
  trip.TripNumber = request.TripNumber!.Trim();
  trip.TripType = request.TripType!.Trim();
  trip.Status = request.Status!.Trim();
  trip.Priority = request.Priority!.Trim();
  trip.CustomerName = request.CustomerName!.Trim();
  trip.Department = request.Department!.Trim();
  trip.CostCenter = NormalizeOptional(request.CostCenter);
  trip.VehicleId = request.VehicleId!.Trim();
  trip.VehiclePlate = request.VehiclePlate!.Trim();
  trip.TrailerNumber = NormalizeOptional(request.TrailerNumber);
  trip.DriverName = request.DriverName!.Trim();
  trip.CoDriverName = NormalizeOptional(request.CoDriverName);
  trip.DispatcherName = request.DispatcherName!.Trim();
  trip.CargoType = request.CargoType!.Trim();
  trip.LoadWeightKg = request.LoadWeightKg;
  trip.LoadVolumeM3 = request.LoadVolumeM3;
  trip.PickupLocation = request.PickupLocation!.Trim();
  trip.DropoffLocation = request.DropoffLocation!.Trim();
  trip.PickupContact = NormalizeOptional(request.PickupContact);
  trip.DropoffContact = NormalizeOptional(request.DropoffContact);
  trip.DepartureDateTime = request.DepartureDateTime!.Trim();
  trip.EstimatedArrival = request.EstimatedArrival!.Trim();
  trip.ActualArrival = NormalizeOptional(request.ActualArrival);
  trip.PlannedDistanceKm = request.PlannedDistanceKm;
  trip.StartingOdometerKm = request.StartingOdometerKm;
  trip.CurrentOdometerKm = request.CurrentOdometerKm;
  trip.EndingOdometerKm = request.EndingOdometerKm;
  trip.FuelIssuedLiters = request.FuelIssuedLiters;
  trip.TollEstimate = request.TollEstimate;
  trip.PermitRequired = request.PermitRequired;
  trip.TemperatureControlled = request.TemperatureControlled;
  trip.TemperatureRange = NormalizeOptional(request.TemperatureRange);
  trip.SpecialInstructions = NormalizeOptional(request.SpecialInstructions);
  trip.DriverNotes = NormalizeOptional(request.DriverNotes);
  return trip;
}

static string? ValidateTripSetupRequest(TripSetupRequest request)
{
  if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
  if (request.Name.Trim().Length > 120) return "Name must be 120 characters or fewer.";
  if (string.IsNullOrWhiteSpace(request.Code)) return "Code is required.";
  if (request.Code.Trim().Length > 40) return "Code must be 40 characters or fewer.";
  if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 500) return "Description must be 500 characters or fewer.";
  var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  return normalizedStatus is "Active" or "Disabled" ? null : "Status must be Active or Disabled.";
}

static async Task<PagedResult<TripSetupDto>> GetTripSetupPage<T>(
  FleetDbContext db,
  int page,
  int pageSize,
  string? search,
  string? sortBy,
  string? sortOrder)
  where T : TripSetupCodeOption
{
  var query = db.Set<T>().AsNoTracking().AsQueryable();
  if (!string.IsNullOrWhiteSpace(search))
  {
    var normalizedSearch = search.Trim().ToLower();
    query = query.Where(option =>
      option.Name.ToLower().Contains(normalizedSearch) ||
      option.Code.ToLower().Contains(normalizedSearch) ||
      (option.Description ?? string.Empty).ToLower().Contains(normalizedSearch) ||
      option.Status.ToLower().Contains(normalizedSearch));
  }
  query = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
  {
    ("name", "desc") => query.OrderByDescending(option => option.Name),
    ("name", _) => query.OrderBy(option => option.Name),
    ("code", "desc") => query.OrderByDescending(option => option.Code),
    ("code", _) => query.OrderBy(option => option.Code),
    ("status", "desc") => query.OrderByDescending(option => option.Status),
    ("status", _) => query.OrderBy(option => option.Status),
    ("id", "desc") => query.OrderByDescending(option => option.Id),
    _ => query.OrderBy(option => option.Id)
  };
  var total = await query.CountAsync();
  var records = await query.Skip(Math.Max(page - 1, 0) * pageSize).Take(pageSize).ToListAsync();
  return new PagedResult<TripSetupDto>(records.Select(ToTripSetupDto).ToList(), total);
}

static async Task<List<string>> GetTripSetupOptions<T>(FleetDbContext db)
  where T : TripSetupCodeOption =>
  await db.Set<T>()
    .AsNoTracking()
    .Where(option => option.Status == "Active")
    .OrderBy(option => option.Name)
    .Select(option => option.Name)
    .ToListAsync();

static async Task<IResult> CreateTripSetupOption<T>(TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db, string moduleKey)
  where T : TripSetupCodeOption, new()
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Create);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateTripSetupRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicate = await db.Set<T>().AnyAsync(option => option.Name.ToLower() == normalizedName.ToLower() || option.Code.ToLower() == normalizedCode.ToLower());
  if (duplicate) return Results.BadRequest(new ApiError("Name or code already exists."));
  var option = new T
  {
    Name = normalizedName,
    Code = normalizedCode,
    Description = NormalizeOptional(request.Description),
    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
    CreatedAt = DateTimeOffset.UtcNow
  };
  db.Set<T>().Add(option);
  await LogAuditAsync(db, httpRequest, moduleKey, "Create", normalizedCode, $"Created setup option {normalizedName}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToTripSetupDto(option));
}

static async Task<IResult> UpdateTripSetupOption<T>(int id, TripSetupRequest request, HttpRequest httpRequest, FleetDbContext db, string moduleKey)
  where T : TripSetupCodeOption
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Edit);
  if (permissionError is not null) return permissionError;

  var validationError = ValidateTripSetupRequest(request);
  if (validationError is not null) return Results.BadRequest(new ApiError(validationError));
  var option = await db.Set<T>().FindAsync(id);
  if (option is null) return Results.NotFound(new ApiError("Setup option not found."));
  var normalizedName = request.Name.Trim();
  var normalizedCode = request.Code.Trim();
  var duplicate = await db.Set<T>().AnyAsync(item => item.Id != id && (item.Name.ToLower() == normalizedName.ToLower() || item.Code.ToLower() == normalizedCode.ToLower()));
  if (duplicate) return Results.BadRequest(new ApiError("Name or code already exists."));
  option.Name = normalizedName;
  option.Code = normalizedCode;
  option.Description = NormalizeOptional(request.Description);
  option.Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();
  option.UpdatedAt = DateTimeOffset.UtcNow;
  await LogAuditAsync(db, httpRequest, moduleKey, "Edit", option.Id.ToString(), $"Updated setup option {normalizedName}.");
  await db.SaveChangesAsync();
  return Results.Ok(ToTripSetupDto(option));
}

static async Task<IResult> DeleteTripSetupOption<T>(int id, HttpRequest httpRequest, FleetDbContext db, string moduleKey)
  where T : TripSetupCodeOption
{
  var permissionError = await RequirePermissionAsync(httpRequest, db, moduleKey, PermissionAction.Delete);
  if (permissionError is not null) return permissionError;

  var option = await db.Set<T>().FindAsync(id);
  if (option is null) return Results.NotFound(new ApiError("Setup option not found."));
  await LogAuditAsync(db, httpRequest, moduleKey, "Delete", option.Id.ToString(), $"Deleted setup option {option.Name}.");
  db.Set<T>().Remove(option);
  await db.SaveChangesAsync();
  return Results.NoContent();
}

static IReadOnlyList<PermissionModuleDefinition> GetPermissionModules() =>
[
  new("dashboard", "Dashboard", "Overview"),
  new("vehicles", "Vehicle Management", "Fleet"),
  new("trips", "Trips", "Fleet"),
  new("maintenance-tickets", "Maintenance Tickets", "Maintenance"),
  new("inventory-parts", "Inventory & Parts", "Maintenance"),
  new("incidents", "Incidents", "Maintenance"),
  new("reports", "Reports", "Reports"),
  new("expenses", "Expenses", "Reports"),
  new("vehicle-documents", "Vehicle Documents", "Compliance"),
  new("driver-documents", "Driver Documents", "Compliance"),
  new("audit-logs", "Audit Logs", "Administration"),
  new("users", "Users", "Administration"),
  new("roles", "Roles", "Administration"),
  new("permissions", "Permissions", "Administration"),
  new("department-setup", "Department Setup", "Setup"),
  new("location-setup", "Location Setup", "Setup"),
  new("location-type-setup", "Location Type Setup", "Setup"),
  new("vehicle-type-setup", "Vehicle Type Setup", "Setup"),
  new("fuel-type-setup", "Fuel Type Setup", "Setup"),
  new("trip-type-setup", "Trip Type Setup", "Setup"),
  new("cargo-type-setup", "Cargo Type Setup", "Setup"),
  new("status-setup", "Status Setup", "Setup"),
  new("trip-priority-setup", "Trip Priority Setup", "Setup"),
  new("incident-type-setup", "Incident Type Setup", "Setup"),
  new("severity-setup", "Severity Setup", "Setup"),
  new("expense-type-setup", "Expense Type Setup", "Setup"),
  new("maintenance-type-setup", "Maintenance Type Setup", "Setup"),
  new("document-type-setup", "Document Type Setup", "Setup"),
  new("supplier-setup", "Supplier Setup", "Setup"),
  new("settings", "Settings", "Administration")
];

static async Task<PermissionMatrixDto> BuildPermissionMatrixAsync(FleetDbContext db)
{
  var fixedRoleIds = SeedData.FixedRoleIds;
  var roles = await db.Roles
    .AsNoTracking()
    .Where(role => role.IsDeleted == 0 && fixedRoleIds.Contains(role.Id))
    .OrderBy(role => role.Code)
    .Select(role => new PermissionRoleDto(role.Id, role.Name))
    .ToListAsync();

  var modules = GetPermissionModules();
  var moduleKeys = modules.Select(module => module.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var permissions = await db.RolePermissions
    .AsNoTracking()
    .Where(permission => fixedRoleIds.Contains(permission.RoleId) && moduleKeys.Contains(permission.ModuleKey))
    .ToListAsync();

  var permissionLookup = permissions.ToDictionary(
    permission => $"{permission.RoleId}:{permission.ModuleKey}",
    StringComparer.OrdinalIgnoreCase);

  var moduleDtos = modules
    .Select(module => new PermissionModuleDto(
      module.Key,
      module.Name,
      module.Category,
      roles.Select(role =>
      {
        var hasPermission = permissionLookup.TryGetValue($"{role.Id}:{module.Key}", out var permission);
        var defaultPermission = GetDefaultPermission(role.Id, module.Key);
        return new RolePermissionDto(
          role.Id,
          permission?.CanView ?? defaultPermission.CanView,
          permission?.CanCreate ?? defaultPermission.CanCreate,
          permission?.CanEdit ?? defaultPermission.CanEdit,
          permission?.CanDelete ?? defaultPermission.CanDelete);
      }).ToList()))
    .ToList();

  return new PermissionMatrixDto(roles, moduleDtos);
}

static async Task<IReadOnlyList<UserPermissionDto>> GetPermissionsForRoleAsync(FleetDbContext db, string roleId)
{
  var modules = GetPermissionModules();
  var moduleKeys = modules.Select(module => module.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var savedPermissions = await db.RolePermissions
    .AsNoTracking()
    .Where(permission => permission.RoleId == roleId && moduleKeys.Contains(permission.ModuleKey))
    .ToListAsync();
  var savedLookup = savedPermissions.ToDictionary(permission => permission.ModuleKey, StringComparer.OrdinalIgnoreCase);

  return modules.Select(module =>
  {
    var hasSaved = savedLookup.TryGetValue(module.Key, out var saved);
    var defaultPermission = GetDefaultPermission(roleId, module.Key);
    return new UserPermissionDto(
      module.Key,
      saved?.CanView ?? defaultPermission.CanView,
      saved?.CanCreate ?? defaultPermission.CanCreate,
      saved?.CanEdit ?? defaultPermission.CanEdit,
      saved?.CanDelete ?? defaultPermission.CanDelete);
  }).ToList();
}

static RolePermissionDto GetDefaultPermission(string roleId, string moduleKey)
{
  if (roleId.Equals("admin", StringComparison.OrdinalIgnoreCase))
  {
    return new RolePermissionDto(roleId, true, true, true, true);
  }

  var viewOnly = new RolePermissionDto(roleId, true, false, false, false);
  var none = new RolePermissionDto(roleId, false, false, false, false);

  return roleId.ToLowerInvariant() switch
  {
    "dispatcher" when moduleKey is "dashboard" or "vehicles" or "trips" or "reports" or "expenses" or "vehicle-documents" or "driver-documents" or "location-setup" => viewOnly with { CanCreate = moduleKey is "trips" or "expenses", CanEdit = moduleKey is "trips" or "expenses" },
    "driver" when moduleKey is "dashboard" or "trips" or "vehicles" or "driver-documents" => viewOnly,
    "mechanic" when moduleKey is "dashboard" or "vehicles" or "maintenance-tickets" or "inventory-parts" or "incidents" or "vehicle-documents" => viewOnly with { CanCreate = moduleKey is "maintenance-tickets" or "incidents", CanEdit = moduleKey is "maintenance-tickets" or "inventory-parts" or "incidents" },
    _ => none
  };
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
  return string.IsNullOrWhiteSpace(normalizedStatus) ? "Ticket status is required." : null;
}

static string? ValidateInventoryPartRequest(InventoryPartForm request)
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

static string? ValidateIncidentRequest(IncidentRequest request)
{
  if (string.IsNullOrWhiteSpace(request.VehicleId)) return "Vehicle is required.";
  if (string.IsNullOrWhiteSpace(request.Driver)) return "Driver is required.";
  if (string.IsNullOrWhiteSpace(request.Date)) return "Incident date is required.";
  if (string.IsNullOrWhiteSpace(request.Type)) return "Incident type is required.";
  if (string.IsNullOrWhiteSpace(request.Severity)) return "Severity is required.";
  if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
  if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000) return "Notes must be 1000 characters or fewer.";
  return null;
}

static string? ValidateExpenseRequest(ExpenseRequest request)
{
  if (string.IsNullOrWhiteSpace(request.ExpenseDate)) return "Expense date is required.";
  if (string.IsNullOrWhiteSpace(request.ExpenseType)) return "Expense type is required.";
  if (request.Amount < 0) return "Expense amount cannot be negative.";
  if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
  if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000) return "Notes must be 1000 characters or fewer.";
  return null;
}

static string? ValidateFleetDocumentRequest(FleetDocumentRequest request)
{
  if (request.OwnerType is not ("Vehicle" or "Driver")) return "Document owner type must be Vehicle or Driver.";
  if (string.IsNullOrWhiteSpace(request.OwnerId)) return "Owner ID is required.";
  if (string.IsNullOrWhiteSpace(request.OwnerName)) return "Owner name is required.";
  if (string.IsNullOrWhiteSpace(request.DocumentType)) return "Document type is required.";
  if (string.IsNullOrWhiteSpace(request.Status)) return "Status is required.";
  if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000) return "Notes must be 1000 characters or fewer.";
  return null;
}

static async Task<T> SafeDashboardValueAsync<T>(Func<Task<T>> load, T fallback)
{
  try
  {
    return await load();
  }
  catch
  {
    return fallback;
  }
}

static Task LogAuditAsync(FleetDbContext db, HttpRequest request, string moduleKey, string action, string entityId, string description)
{
  db.AuditLogs.Add(new AuditLog
  {
    RoleId = GetRequestRoleId(request),
    ModuleKey = moduleKey,
    Action = action,
    EntityId = entityId,
    Description = description,
    CreatedAt = DateTime.UtcNow
  });
  return Task.CompletedTask;
}

static void AddStatusHistoryIfChanged(FleetDbContext db, HttpRequest request, string entityType, string entityId, string? oldStatus, string newStatus)
{
  if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase)) return;
  db.StatusHistories.Add(new StatusHistory
  {
    EntityType = entityType,
    EntityId = entityId,
    OldStatus = oldStatus,
    NewStatus = newStatus,
    RoleId = GetRequestRoleId(request),
    CreatedAt = DateTime.UtcNow
  });
}

static string GetRequestRoleId(HttpRequest request) =>
  request.Headers.TryGetValue("X-Fleet-Role-Id", out var roleId) && !string.IsNullOrWhiteSpace(roleId.ToString())
    ? roleId.ToString()
    : "system";

static IEnumerable<DashboardUpcomingExpiryDto> GetUpcomingVehicleExpiries(Vehicle vehicle)
{
  foreach (var expiry in GetVehicleExpiryCandidates(vehicle))
  {
    if (!DateTime.TryParse(expiry.Date, out var parsedDate)) continue;
    var daysRemaining = (parsedDate.Date - DateTime.UtcNow.Date).Days;
    if (daysRemaining < 0 || daysRemaining > 60) continue;
    yield return new DashboardUpcomingExpiryDto("Vehicle", $"{vehicle.Id} {expiry.Label}", parsedDate.ToString("yyyy-MM-dd"), daysRemaining);
  }
}

static IEnumerable<(string Label, string? Date)> GetVehicleExpiryCandidates(Vehicle vehicle)
{
  yield return ("registration", vehicle.RegistrationExpiry);
  yield return ("road tax", vehicle.RoadTaxExpiry);
  yield return ("insurance", vehicle.InsuranceExpiry);
  yield return ("inspection", vehicle.InspectionDue);
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

public record PermissionModuleDefinition(string Key, string Name, string Category);

enum PermissionAction
{
  View,
  Create,
  Edit,
  Delete
}
