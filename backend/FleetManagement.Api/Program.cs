using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueClient", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FleetDatabase")));

var app = builder.Build();

app.UseCors("VueClient");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    await db.Database.EnsureCreatedAsync();
    await EnsureUserSchemaAsync(db);
    await SeedData.EnsureSeededAsync(db);
}

var roles = app.MapGroup("/api/roles");

roles.MapGet("/", async (FleetDbContext db) =>
{
    var items = await db.Roles
        .AsNoTracking()
        .OrderBy(role => role.Name)
        .Select(role => new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.Status,
            role.CreatedAt,
            role.UpdatedAt,
            role.Users.Count))
        .ToListAsync();

    return Results.Ok(items);
});

roles.MapGet("/{id:int}", async (int id, FleetDbContext db) =>
{
    var role = await db.Roles
        .AsNoTracking()
        .Where(role => role.Id == id)
        .Select(role => new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.Status,
            role.CreatedAt,
            role.UpdatedAt,
            role.Users.Count))
        .FirstOrDefaultAsync();

    return role is null ? Results.NotFound() : Results.Ok(role);
});

roles.MapGet("/{id:int}/members", async (int id, FleetDbContext db) =>
{
    var roleExists = await db.Roles.AnyAsync(role => role.Id == id);
    if (!roleExists) return Results.NotFound();

    var members = await db.Users
        .AsNoTracking()
        .Where(user => user.RoleId == id)
        .OrderBy(user => user.Name)
        .Select(user => new RoleMemberDto(
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Status,
            user.JoinDate,
            user.Avatar))
        .ToListAsync();

    return Results.Ok(members);
});

roles.MapPost("/", async (RoleRequest request, FleetDbContext db) =>
{
    var validationError = ValidateRoleRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var nameExists = await db.Roles.AnyAsync(role => role.Name == request.Name.Trim());
    if (nameExists) return Results.Conflict(new { message = "Role name already exists." });

    var role = new Role
    {
        Name = request.Name.Trim(),
        Description = request.Description.Trim(),
        Status = request.Status.Trim(),
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.Roles.Add(role);
    await db.SaveChangesAsync();

    return Results.Created($"/api/roles/{role.Id}", new RoleDto(
        role.Id,
        role.Name,
        role.Description,
        role.Status,
        role.CreatedAt,
        role.UpdatedAt,
        0));
});

roles.MapPut("/{id:int}", async (int id, RoleRequest request, FleetDbContext db) =>
{
    var validationError = ValidateRoleRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var role = await db.Roles.FindAsync(id);
    if (role is null) return Results.NotFound();

    var nextName = request.Name.Trim();
    var nameExists = await db.Roles.AnyAsync(item => item.Id != id && item.Name == nextName);
    if (nameExists) return Results.Conflict(new { message = "Role name already exists." });

    role.Name = nextName;
    role.Description = request.Description.Trim();
    role.Status = request.Status.Trim();
    role.UpdatedAt = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync();

    var members = await db.Users.CountAsync(user => user.RoleId == role.Id);

    return Results.Ok(new RoleDto(
        role.Id,
        role.Name,
        role.Description,
        role.Status,
        role.CreatedAt,
        role.UpdatedAt,
        members));
});

roles.MapDelete("/{id:int}", async (int id, FleetDbContext db) =>
{
    var role = await db.Roles.FindAsync(id);
    if (role is null) return Results.NotFound();

    var hasMembers = await db.Users.AnyAsync(user => user.RoleId == id);
    if (hasMembers)
    {
        return Results.Conflict(new { message = "Cannot delete a role while users are assigned to it." });
    }

    db.Roles.Remove(role);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

var users = app.MapGroup("/api/users");

users.MapGet("/", async (FleetDbContext db) =>
{
    var userItems = await db.Users
        .AsNoTracking()
        .Include(user => user.Role)
        .OrderBy(user => user.Name)
        .ToListAsync();

    return Results.Ok(userItems.Select(ToUserDto));
});

users.MapGet("/{id:int}", async (int id, FleetDbContext db) =>
{
    var user = await db.Users
        .AsNoTracking()
        .Include(item => item.Role)
        .FirstOrDefaultAsync(item => item.Id == id);

    return user is null ? Results.NotFound() : Results.Ok(ToUserDto(user));
});

users.MapPost("/", async (UserRequest request, FleetDbContext db) =>
{
    var validationError = ValidateUserRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var role = await FindRoleByNameAsync(request.Role, db);
    if (role is null) return Results.BadRequest(new { message = "Selected role does not exist." });

    var duplicateError = await ValidateUniqueUserFieldsAsync(request, null, db);
    if (duplicateError is not null) return Results.Conflict(new { message = duplicateError });

    var user = new User
    {
        Name = request.Name.Trim(),
        EmployeeId = request.EmployeeId.Trim(),
        NrcNumber = request.NrcNumber.Trim(),
        Email = request.Email.Trim(),
        Phone = request.Phone.Trim(),
        Status = request.Status.Trim(),
        JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
        LastLogin = DateTimeOffset.UtcNow,
        Avatar = NormalizeOptional(request.Avatar),
        NrcFront = NormalizeOptional(request.NrcFront),
        NrcBack = NormalizeOptional(request.NrcBack),
        Department = request.Department.Trim(),
        Title = request.Title.Trim(),
        Location = request.Location.Trim(),
        Manager = request.Manager.Trim(),
        LicenseNumber = NormalizeOptional(request.LicenseNumber),
        LicenseClass = NormalizeOptional(request.LicenseClass),
        LicenseExpiry = request.LicenseExpiry,
        EmergencyContactName = request.EmergencyContactName.Trim(),
        EmergencyContactRelation = request.EmergencyContactRelation.Trim(),
        EmergencyContactPhone = request.EmergencyContactPhone.Trim(),
        Address = request.Address.Trim(),
        TwoFactorEnabled = request.TwoFactorEnabled,
        Notes = NormalizeOptional(request.Notes),
        RoleId = role.Id,
        Role = role
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", ToUserDto(user));
});

users.MapPut("/{id:int}", async (int id, UserRequest request, FleetDbContext db) =>
{
    var validationError = ValidateUserRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var user = await db.Users
        .Include(item => item.Role)
        .FirstOrDefaultAsync(item => item.Id == id);
    if (user is null) return Results.NotFound();

    var role = await FindRoleByNameAsync(request.Role, db);
    if (role is null) return Results.BadRequest(new { message = "Selected role does not exist." });

    var duplicateError = await ValidateUniqueUserFieldsAsync(request, id, db);
    if (duplicateError is not null) return Results.Conflict(new { message = duplicateError });

    user.Name = request.Name.Trim();
    user.EmployeeId = request.EmployeeId.Trim();
    user.NrcNumber = request.NrcNumber.Trim();
    user.Email = request.Email.Trim();
    user.Phone = request.Phone.Trim();
    user.Status = request.Status.Trim();
    user.Avatar = NormalizeOptional(request.Avatar);
    user.NrcFront = NormalizeOptional(request.NrcFront);
    user.NrcBack = NormalizeOptional(request.NrcBack);
    user.Department = request.Department.Trim();
    user.Title = request.Title.Trim();
    user.Location = request.Location.Trim();
    user.Manager = request.Manager.Trim();
    user.LicenseNumber = NormalizeOptional(request.LicenseNumber);
    user.LicenseClass = NormalizeOptional(request.LicenseClass);
    user.LicenseExpiry = request.LicenseExpiry;
    user.EmergencyContactName = request.EmergencyContactName.Trim();
    user.EmergencyContactRelation = request.EmergencyContactRelation.Trim();
    user.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
    user.Address = request.Address.Trim();
    user.TwoFactorEnabled = request.TwoFactorEnabled;
    user.Notes = NormalizeOptional(request.Notes);
    user.RoleId = role.Id;
    user.Role = role;

    await db.SaveChangesAsync();

    return Results.Ok(ToUserDto(user));
});

users.MapPatch("/{id:int}/status", async (int id, UserStatusRequest request, FleetDbContext db) =>
{
    var status = request.Status.Trim();
    if (status is not ("Active" or "Disabled"))
    {
        return Results.BadRequest(new { message = "User status must be Active or Disabled." });
    }

    var user = await db.Users
        .Include(item => item.Role)
        .FirstOrDefaultAsync(item => item.Id == id);
    if (user is null) return Results.NotFound();

    user.Status = status;
    await db.SaveChangesAsync();

    return Results.Ok(ToUserDto(user));
});

users.MapDelete("/{id:int}", async (int id, FleetDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();

static string? ValidateRoleRequest(RoleRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name)) return "Role name is required.";
    if (string.IsNullOrWhiteSpace(request.Description)) return "Role description is required.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Role status is required.";

    var status = request.Status.Trim();
    return status is "Active" or "Disabled"
        ? null
        : "Role status must be Active or Disabled.";
}

static UserDto ToUserDto(User user) => new(
    user.Id,
    user.Name,
    user.EmployeeId,
    user.NrcNumber,
    user.Email,
    user.Role?.Name ?? "",
    user.Status,
    user.Phone,
    user.Avatar,
    user.NrcFront,
    user.NrcBack,
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
    user.LastLogin,
    user.TwoFactorEnabled,
    user.Notes,
    user.JoinDate);

static async Task<Role?> FindRoleByNameAsync(string roleName, FleetDbContext db)
{
    var name = roleName.Trim();
    return await db.Roles.FirstOrDefaultAsync(role => role.Name == name);
}

static async Task<string?> ValidateUniqueUserFieldsAsync(UserRequest request, int? userId, FleetDbContext db)
{
    var employeeId = request.EmployeeId.Trim();
    var nrcNumber = request.NrcNumber.Trim();
    var email = request.Email.Trim();

    var exists = await db.Users.AnyAsync(user =>
        (!userId.HasValue || user.Id != userId.Value) &&
        (user.EmployeeId == employeeId || user.NrcNumber == nrcNumber || user.Email == email));

    if (!exists) return null;

    if (await db.Users.AnyAsync(user => (!userId.HasValue || user.Id != userId.Value) && user.EmployeeId == employeeId))
    {
        return "Employee ID already exists.";
    }

    if (await db.Users.AnyAsync(user => (!userId.HasValue || user.Id != userId.Value) && user.NrcNumber == nrcNumber))
    {
        return "NRC number already exists.";
    }

    return "Email already exists.";
}

static string? ValidateUserRequest(UserRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name)) return "Full name is required.";
    if (string.IsNullOrWhiteSpace(request.EmployeeId)) return "Employee ID is required.";
    if (string.IsNullOrWhiteSpace(request.NrcNumber)) return "NRC is required.";
    if (!Regex.IsMatch(request.NrcNumber.Trim(), @"^\d{1,2}/[A-Za-z]+/\d{6}$"))
    {
        return "NRC format must be like 9/ZaYaTha/111111.";
    }
    if (string.IsNullOrWhiteSpace(request.Email)) return "Email is required.";
    if (string.IsNullOrWhiteSpace(request.Phone)) return "Phone number is required.";
    if (string.IsNullOrWhiteSpace(request.Role)) return "Role is required.";
    if (request.Status.Trim() is not ("Active" or "Disabled")) return "User status must be Active or Disabled.";
    if (string.IsNullOrWhiteSpace(request.Title)) return "Job title is required.";
    if (string.IsNullOrWhiteSpace(request.Department)) return "Department is required.";
    if (string.IsNullOrWhiteSpace(request.Location)) return "Location is required.";
    if (string.IsNullOrWhiteSpace(request.Manager)) return "Manager is required.";

    if (request.Role.Trim() == "Driver")
    {
        if (string.IsNullOrWhiteSpace(request.LicenseNumber)) return "License number is required for drivers.";
        if (string.IsNullOrWhiteSpace(request.LicenseClass)) return "License class is required for drivers.";
        if (request.LicenseExpiry is null) return "License expiry is required for drivers.";
    }

    if (string.IsNullOrWhiteSpace(request.EmergencyContactName)) return "Emergency contact name is required.";
    if (string.IsNullOrWhiteSpace(request.EmergencyContactRelation)) return "Emergency contact relation is required.";
    if (string.IsNullOrWhiteSpace(request.EmergencyContactPhone)) return "Emergency contact phone is required.";
    if (string.IsNullOrWhiteSpace(request.Address)) return "Address is required.";
    if (string.IsNullOrWhiteSpace(request.Avatar)) return "Profile image is required.";
    if (string.IsNullOrWhiteSpace(request.NrcFront)) return "NRC front image is required.";
    if (string.IsNullOrWhiteSpace(request.NrcBack)) return "NRC back image is required.";

    return null;
}

static string? NormalizeOptional(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

static async Task EnsureUserSchemaAsync(FleetDbContext db)
{
    var sql = """
IF COL_LENGTH('Users', 'EmployeeId') IS NULL ALTER TABLE [Users] ADD [EmployeeId] nvarchar(40) NOT NULL CONSTRAINT DF_Users_EmployeeId DEFAULT '';
IF COL_LENGTH('Users', 'NrcNumber') IS NULL ALTER TABLE [Users] ADD [NrcNumber] nvarchar(80) NOT NULL CONSTRAINT DF_Users_NrcNumber DEFAULT '';
IF COL_LENGTH('Users', 'LastLogin') IS NULL ALTER TABLE [Users] ADD [LastLogin] datetimeoffset NULL;
IF COL_LENGTH('Users', 'NrcFront') IS NULL ALTER TABLE [Users] ADD [NrcFront] nvarchar(max) NULL;
IF COL_LENGTH('Users', 'NrcBack') IS NULL ALTER TABLE [Users] ADD [NrcBack] nvarchar(max) NULL;
IF COL_LENGTH('Users', 'Department') IS NULL ALTER TABLE [Users] ADD [Department] nvarchar(100) NOT NULL CONSTRAINT DF_Users_Department DEFAULT '';
IF COL_LENGTH('Users', 'Title') IS NULL ALTER TABLE [Users] ADD [Title] nvarchar(100) NOT NULL CONSTRAINT DF_Users_Title DEFAULT '';
IF COL_LENGTH('Users', 'Location') IS NULL ALTER TABLE [Users] ADD [Location] nvarchar(120) NOT NULL CONSTRAINT DF_Users_Location DEFAULT '';
IF COL_LENGTH('Users', 'Manager') IS NULL ALTER TABLE [Users] ADD [Manager] nvarchar(120) NOT NULL CONSTRAINT DF_Users_Manager DEFAULT '';
IF COL_LENGTH('Users', 'LicenseNumber') IS NULL ALTER TABLE [Users] ADD [LicenseNumber] nvarchar(80) NULL;
IF COL_LENGTH('Users', 'LicenseClass') IS NULL ALTER TABLE [Users] ADD [LicenseClass] nvarchar(40) NULL;
IF COL_LENGTH('Users', 'LicenseExpiry') IS NULL ALTER TABLE [Users] ADD [LicenseExpiry] date NULL;
IF COL_LENGTH('Users', 'EmergencyContactName') IS NULL ALTER TABLE [Users] ADD [EmergencyContactName] nvarchar(120) NOT NULL CONSTRAINT DF_Users_EmergencyContactName DEFAULT '';
IF COL_LENGTH('Users', 'EmergencyContactRelation') IS NULL ALTER TABLE [Users] ADD [EmergencyContactRelation] nvarchar(80) NOT NULL CONSTRAINT DF_Users_EmergencyContactRelation DEFAULT '';
IF COL_LENGTH('Users', 'EmergencyContactPhone') IS NULL ALTER TABLE [Users] ADD [EmergencyContactPhone] nvarchar(40) NOT NULL CONSTRAINT DF_Users_EmergencyContactPhone DEFAULT '';
IF COL_LENGTH('Users', 'Address') IS NULL ALTER TABLE [Users] ADD [Address] nvarchar(300) NOT NULL CONSTRAINT DF_Users_Address DEFAULT '';
IF COL_LENGTH('Users', 'TwoFactorEnabled') IS NULL ALTER TABLE [Users] ADD [TwoFactorEnabled] bit NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT 0;
IF COL_LENGTH('Users', 'Notes') IS NULL ALTER TABLE [Users] ADD [Notes] nvarchar(1000) NULL;
IF COL_LENGTH('Users', 'Avatar') IS NOT NULL ALTER TABLE [Users] ALTER COLUMN [Avatar] nvarchar(max) NULL;

UPDATE [Users]
SET
    [EmployeeId] = CASE WHEN [EmployeeId] = '' THEN CONCAT('EMP-', RIGHT(CONCAT('0000', [Id]), 4)) ELSE [EmployeeId] END,
    [NrcNumber] = CASE WHEN [NrcNumber] = '' THEN CONCAT('12/ZaYaTha/', RIGHT(CONCAT('000000', [Id]), 6)) ELSE [NrcNumber] END,
    [Department] = CASE WHEN [Department] = '' THEN 'Operations' ELSE [Department] END,
    [Title] = CASE WHEN [Title] = '' THEN 'Fleet Staff' ELSE [Title] END,
    [Location] = CASE WHEN [Location] = '' THEN 'HQ' ELSE [Location] END,
    [Manager] = CASE WHEN [Manager] = '' THEN 'Admin User' ELSE [Manager] END,
    [EmergencyContactName] = CASE WHEN [EmergencyContactName] = '' THEN 'Emergency Contact' ELSE [EmergencyContactName] END,
    [EmergencyContactRelation] = CASE WHEN [EmergencyContactRelation] = '' THEN 'Other' ELSE [EmergencyContactRelation] END,
    [EmergencyContactPhone] = CASE WHEN [EmergencyContactPhone] = '' THEN [Phone] ELSE [EmergencyContactPhone] END,
    [Address] = CASE WHEN [Address] = '' THEN 'Address pending' ELSE [Address] END,
    [Avatar] = COALESCE([Avatar], 'https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=facearea&w=160&h=160&q=80'),
    [NrcFront] = COALESCE([NrcFront], 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw=='),
    [NrcBack] = COALESCE([NrcBack], 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw=='),
    [LastLogin] = COALESCE([LastLogin], SYSDATETIMEOFFSET());
""";

    await db.Database.ExecuteSqlRawAsync(sql);
}
