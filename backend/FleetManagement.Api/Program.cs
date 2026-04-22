using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Net.Mail;
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

app.UseStaticFiles();
app.UseCors("VueClient");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    await db.Database.EnsureCreatedAsync();
    await EnsureUserSchemaAsync(db);
    await EnsureUserCodeOptionSchemaAsync(db);
    await EnsureSeparatedUserCodeOptionSchemaAsync(db);
    await SeedData.EnsureSeededAsync(db);
    await MigrateUserImagesToFileStorageAsync(db, app.Environment);
}

var roles = app.MapGroup("/api/roles");

roles.MapGet("/", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, string? sortOrder = null) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var filteredQuery = db.Roles.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        filteredQuery = filteredQuery.Where(role => role.Name.Contains(term) || role.Description.Contains(term));
    }

    var total = await filteredQuery.CountAsync();
    var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
    var normalizedSort = (sortBy ?? "id").Trim().ToLowerInvariant();
    var query = filteredQuery;
    query = normalizedSort switch
    {
        "id" => descending ? query.OrderByDescending(role => role.Id) : query.OrderBy(role => role.Id),
        "description" => descending ? query.OrderByDescending(role => role.Description) : query.OrderBy(role => role.Description),
        "status" => descending ? query.OrderByDescending(role => role.Status) : query.OrderBy(role => role.Status),
        "createdat" => descending ? query.OrderByDescending(role => role.CreatedAt) : query.OrderBy(role => role.CreatedAt),
        "members" => descending ? query.OrderByDescending(role => role.Users.Count) : query.OrderBy(role => role.Users.Count),
        _ => descending ? query.OrderByDescending(role => role.Name) : query.OrderBy(role => role.Name)
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(role => new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.Status,
            role.CreatedAt,
            role.UpdatedAt,
            role.Users.Count,
            filteredQuery.Count(item => item.Id <= role.Id)))
        .ToListAsync();

    return Results.Ok(new PagedResult<RoleDto>(items, total, page, pageSize));
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
            role.Users.Count,
            null))
        .FirstOrDefaultAsync();

    return role is null ? Results.NotFound() : Results.Ok(role);
});

roles.MapGet("/options", async (FleetDbContext db) =>
{
    var items = await db.Roles
        .AsNoTracking()
        .OrderBy(role => role.Name)
        .Select(role => role.Name)
        .ToListAsync();

    return Results.Ok(items);
});

roles.MapGet("/{id:int}/members", async (int id, FleetDbContext db, HttpContext httpContext) =>
{
    var roleExists = await db.Roles.AnyAsync(role => role.Id == id);
    if (!roleExists) return Results.NotFound();

    var members = await db.Users
        .AsNoTracking()
        .Where(user => user.RoleId == id)
        .OrderBy(user => user.Name)
        .Select(user => new RoleMemberListItem(
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Status,
            user.JoinDate,
            user.Avatar))
        .ToListAsync();

    return Results.Ok(members.Select(member => new RoleMemberDto(
        member.Id,
        member.Name,
        member.Email,
        member.Phone,
        member.Status,
        member.JoinDate,
        ResolveStoredImageUrl(httpContext, member.Avatar, member.Id, "avatar"))));
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

var userCodeOptions = app.MapGroup("/api/user-code-options");

userCodeOptions.MapGet("/departments", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, string? sortOrder = null) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var filteredQuery = db.DepartmentCodeOptions.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        filteredQuery = filteredQuery.Where(option =>
            option.Name.Contains(term) ||
            (option.Description != null && option.Description.Contains(term)));
    }

    var total = await filteredQuery.CountAsync();
    var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
    var normalizedSort = (sortBy ?? "id").Trim().ToLowerInvariant();
    var query = filteredQuery;
    query = normalizedSort switch
    {
        "id" => descending ? query.OrderByDescending(option => option.Id) : query.OrderBy(option => option.Id),
        "description" => descending ? query.OrderByDescending(option => option.Description) : query.OrderBy(option => option.Description),
        "status" => descending ? query.OrderByDescending(option => option.Status) : query.OrderBy(option => option.Status),
        "createdat" => descending ? query.OrderByDescending(option => option.CreatedAt) : query.OrderBy(option => option.CreatedAt),
        _ => descending ? query.OrderByDescending(option => option.Name) : query.OrderBy(option => option.Name)
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(option => new UserCodeOptionDto(
            option.Id,
            "Department",
            option.Name,
            option.Description,
            option.Status,
            option.CreatedAt,
            option.UpdatedAt,
            filteredQuery.Count(item => item.Id <= option.Id)))
        .ToListAsync();

    return Results.Ok(new PagedResult<UserCodeOptionDto>(items, total, page, pageSize));
});

userCodeOptions.MapGet("/locations", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, string? sortOrder = null) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var filteredQuery = db.LocationCodeOptions.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        filteredQuery = filteredQuery.Where(option =>
            option.Name.Contains(term) ||
            (option.Description != null && option.Description.Contains(term)));
    }

    var total = await filteredQuery.CountAsync();
    var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
    var normalizedSort = (sortBy ?? "name").Trim().ToLowerInvariant();
    var query = filteredQuery;
    query = normalizedSort switch
    {
        "id" => descending ? query.OrderByDescending(option => option.Id) : query.OrderBy(option => option.Id),
        "description" => descending ? query.OrderByDescending(option => option.Description) : query.OrderBy(option => option.Description),
        "status" => descending ? query.OrderByDescending(option => option.Status) : query.OrderBy(option => option.Status),
        "createdat" => descending ? query.OrderByDescending(option => option.CreatedAt) : query.OrderBy(option => option.CreatedAt),
        _ => descending ? query.OrderByDescending(option => option.Name) : query.OrderBy(option => option.Name)
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(option => new UserCodeOptionDto(
            option.Id,
            "Location",
            option.Name,
            option.Description,
            option.Status,
            option.CreatedAt,
            option.UpdatedAt,
            filteredQuery.Count(item => item.Id <= option.Id)))
        .ToListAsync();

    return Results.Ok(new PagedResult<UserCodeOptionDto>(items, total, page, pageSize));
});

userCodeOptions.MapGet("/", async (FleetDbContext db, int page = 1, int pageSize = 10, string? search = null, string? type = null, string? sortBy = null, string? sortOrder = null) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var normalizedType = string.IsNullOrWhiteSpace(type) || type == "All" ? null : NormalizeUserCodeOptionType(type);
    if (type is not null && type != "All" && normalizedType is null)
    {
        return Results.BadRequest(new { message = "Type must be Department or Location." });
    }

    var departments = db.DepartmentCodeOptions.AsNoTracking();
    var locations = db.LocationCodeOptions.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        departments = departments.Where(option =>
            option.Name.Contains(term) ||
            (option.Description != null && option.Description.Contains(term)));
        locations = locations.Where(option =>
            option.Name.Contains(term) ||
            (option.Description != null && option.Description.Contains(term)));
    }

    IQueryable<UserCodeOptionDto> departmentItems = departments.Select(option => new UserCodeOptionDto(
        option.Id,
        "Department",
        option.Name,
        option.Description,
        option.Status,
        option.CreatedAt,
        option.UpdatedAt,
        departments.Count(item => item.Id <= option.Id)));

    IQueryable<UserCodeOptionDto> locationItems = locations.Select(option => new UserCodeOptionDto(
        option.Id,
        "Location",
        option.Name,
        option.Description,
        option.Status,
        option.CreatedAt,
        option.UpdatedAt,
        locations.Count(item => item.Id <= option.Id)));

    var merged = normalizedType switch
    {
        "Department" => departmentItems,
        "Location" => locationItems,
        _ => departmentItems.Concat(locationItems)
    };

    var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
    var normalizedSort = (sortBy ?? "name").Trim().ToLowerInvariant();
    merged = normalizedSort switch
    {
        "id" => descending ? merged.OrderByDescending(option => option.Id) : merged.OrderBy(option => option.Id),
        "type" => descending ? merged.OrderByDescending(option => option.Type) : merged.OrderBy(option => option.Type),
        "description" => descending ? merged.OrderByDescending(option => option.Description) : merged.OrderBy(option => option.Description),
        "status" => descending ? merged.OrderByDescending(option => option.Status) : merged.OrderBy(option => option.Status),
        "createdat" => descending ? merged.OrderByDescending(option => option.CreatedAt) : merged.OrderBy(option => option.CreatedAt),
        _ => descending ? merged.OrderByDescending(option => option.Name) : merged.OrderBy(option => option.Name)
    };

    var total = await merged.CountAsync();
    var items = await merged
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new PagedResult<UserCodeOptionDto>(items, total, page, pageSize));
});

userCodeOptions.MapGet("/options", async (FleetDbContext db, string? type = null) =>
{
    var normalizedType = string.IsNullOrWhiteSpace(type) ? null : NormalizeUserCodeOptionType(type);
    if (type is not null && normalizedType is null)
    {
        return Results.BadRequest(new { message = "Type must be Department or Location." });
    }

    List<string> items;
    if (normalizedType == "Department")
    {
        items = await db.DepartmentCodeOptions
            .AsNoTracking()
            .Where(option => option.Status == "Active")
            .OrderBy(option => option.Name)
            .Select(option => option.Name)
            .ToListAsync();
    }
    else if (normalizedType == "Location")
    {
        items = await db.LocationCodeOptions
            .AsNoTracking()
            .Where(option => option.Status == "Active")
            .OrderBy(option => option.Name)
            .Select(option => option.Name)
            .ToListAsync();
    }
    else
    {
        var departmentItems = await db.DepartmentCodeOptions
            .AsNoTracking()
            .Where(option => option.Status == "Active")
            .Select(option => option.Name)
            .ToListAsync();
        var locationItems = await db.LocationCodeOptions
            .AsNoTracking()
            .Where(option => option.Status == "Active")
            .Select(option => option.Name)
            .ToListAsync();
        items = departmentItems.Concat(locationItems).OrderBy(name => name).ToList();
    }

    return Results.Ok(items);
});

userCodeOptions.MapPost("/", async (UserCodeOptionRequest request, FleetDbContext db) =>
{
    var validationError = ValidateUserCodeOptionRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var normalizedType = NormalizeUserCodeOptionType(request.Type)!;
    var normalizedName = request.Name.Trim();

    var duplicateExists = normalizedType == "Department"
        ? await db.DepartmentCodeOptions.AnyAsync(option => option.Name == normalizedName)
        : await db.LocationCodeOptions.AnyAsync(option => option.Name == normalizedName);
    if (duplicateExists) return Results.Conflict(new { message = $"{normalizedType} already exists." });

    var now = DateTimeOffset.UtcNow;
    if (normalizedType == "Department")
    {
        var option = new DepartmentCodeOption
        {
            Name = normalizedName,
            Description = NormalizeOptional(request.Description),
            Status = request.Status.Trim(),
            CreatedAt = now
        };
        db.DepartmentCodeOptions.Add(option);
        await db.SaveChangesAsync();
        return Results.Created($"/api/user-code-options/{option.Id}", new UserCodeOptionDto(
            option.Id,
            "Department",
            option.Name,
            option.Description,
            option.Status,
            option.CreatedAt,
            option.UpdatedAt));
    }

    var locationOption = new LocationCodeOption
    {
        Name = normalizedName,
        Description = NormalizeOptional(request.Description),
        Status = request.Status.Trim(),
        CreatedAt = now
    };
    db.LocationCodeOptions.Add(locationOption);
    await db.SaveChangesAsync();

    return Results.Created($"/api/user-code-options/{locationOption.Id}", new UserCodeOptionDto(
        locationOption.Id,
        "Location",
        locationOption.Name,
        locationOption.Description,
        locationOption.Status,
        locationOption.CreatedAt,
        locationOption.UpdatedAt));
});

userCodeOptions.MapPut("/{id:int}", async (int id, UserCodeOptionRequest request, FleetDbContext db) =>
{
    var validationError = ValidateUserCodeOptionRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var normalizedType = NormalizeUserCodeOptionType(request.Type)!;
    var normalizedName = request.Name.Trim();
    var now = DateTimeOffset.UtcNow;

    if (normalizedType == "Department")
    {
        var option = await db.DepartmentCodeOptions.FindAsync(id);
        if (option is null) return Results.NotFound();

        var duplicateExists = await db.DepartmentCodeOptions.AnyAsync(item => item.Id != id && item.Name == normalizedName);
        if (duplicateExists) return Results.Conflict(new { message = "Department already exists." });

        option.Name = normalizedName;
        option.Description = NormalizeOptional(request.Description);
        option.Status = request.Status.Trim();
        option.UpdatedAt = now;
        await db.SaveChangesAsync();

        return Results.Ok(new UserCodeOptionDto(
            option.Id,
            "Department",
            option.Name,
            option.Description,
            option.Status,
            option.CreatedAt,
            option.UpdatedAt));
    }

    var locationOption = await db.LocationCodeOptions.FindAsync(id);
    if (locationOption is null) return Results.NotFound();

    var locationDuplicateExists = await db.LocationCodeOptions.AnyAsync(item => item.Id != id && item.Name == normalizedName);
    if (locationDuplicateExists) return Results.Conflict(new { message = "Location already exists." });

    locationOption.Name = normalizedName;
    locationOption.Description = NormalizeOptional(request.Description);
    locationOption.Status = request.Status.Trim();
    locationOption.UpdatedAt = now;
    await db.SaveChangesAsync();

    return Results.Ok(new UserCodeOptionDto(
        locationOption.Id,
        "Location",
        locationOption.Name,
        locationOption.Description,
        locationOption.Status,
        locationOption.CreatedAt,
        locationOption.UpdatedAt));
});

userCodeOptions.MapDelete("/{id:int}", async (int id, FleetDbContext db, string? type = null) =>
{
    var normalizedType = NormalizeUserCodeOptionType(type);
    if (!string.IsNullOrWhiteSpace(type) && normalizedType is null)
    {
        return Results.BadRequest(new { message = "Type must be Department or Location." });
    }

    if (normalizedType is null or "Department")
    {
        var departmentOption = await db.DepartmentCodeOptions.FindAsync(id);
        if (departmentOption is not null)
        {
            var isInUse = await db.Users.AnyAsync(user => user.Department == departmentOption.Name);
            if (isInUse)
            {
                return Results.Conflict(new { message = "Cannot delete department while it is assigned to users." });
            }

            db.DepartmentCodeOptions.Remove(departmentOption);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }
    }

    if (normalizedType is null or "Location")
    {
        var locationOption = await db.LocationCodeOptions.FindAsync(id);
        if (locationOption is null) return Results.NotFound();

        var locationInUse = await db.Users.AnyAsync(user => user.Location == locationOption.Name);
        if (locationInUse)
        {
            return Results.Conflict(new { message = "Cannot delete location while it is assigned to users." });
        }

        db.LocationCodeOptions.Remove(locationOption);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
    
    return Results.NotFound();
});

var users = app.MapGroup("/api/users");

users.MapGet("/", async (FleetDbContext db, HttpContext httpContext, int page = 1, int pageSize = 10, string? search = null, string? role = null, string? sortBy = null, string? sortOrder = null) =>
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var filteredQuery = db.Users.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        filteredQuery = filteredQuery.Where(user =>
            user.Name.Contains(term) ||
            user.Email.Contains(term) ||
            user.NrcNumber.Contains(term) ||
            user.EmployeeId.Contains(term) ||
            user.Phone.Contains(term) ||
            user.Department.Contains(term) ||
            user.Title.Contains(term) ||
            user.Location.Contains(term));
    }

    if (!string.IsNullOrWhiteSpace(role) && role != "All")
    {
        var roleName = role.Trim();
        filteredQuery = filteredQuery.Where(user => user.Role!.Name == roleName);
    }

    var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
    var normalizedSort = (sortBy ?? "name").Trim().ToLowerInvariant();
    var query = filteredQuery;
    query = normalizedSort switch
    {
        "id" => descending ? query.OrderByDescending(user => user.Id) : query.OrderBy(user => user.Id),
        "employeeid" => descending ? query.OrderByDescending(user => user.EmployeeId) : query.OrderBy(user => user.EmployeeId),
        "nrcnumber" => descending ? query.OrderByDescending(user => user.NrcNumber) : query.OrderBy(user => user.NrcNumber),
        "email" => descending ? query.OrderByDescending(user => user.Email) : query.OrderBy(user => user.Email),
        "role" => descending ? query.OrderByDescending(user => user.Role!.Name) : query.OrderBy(user => user.Role!.Name),
        "phone" => descending ? query.OrderByDescending(user => user.Phone) : query.OrderBy(user => user.Phone),
        "department" => descending ? query.OrderByDescending(user => user.Department) : query.OrderBy(user => user.Department),
        "title" => descending ? query.OrderByDescending(user => user.Title) : query.OrderBy(user => user.Title),
        "location" => descending ? query.OrderByDescending(user => user.Location) : query.OrderBy(user => user.Location),
        "manager" => descending ? query.OrderByDescending(user => user.Manager) : query.OrderBy(user => user.Manager),
        "status" => descending ? query.OrderByDescending(user => user.Status) : query.OrderBy(user => user.Status),
        "joindate" => descending ? query.OrderByDescending(user => user.JoinDate) : query.OrderBy(user => user.JoinDate),
        "lastlogin" => descending ? query.OrderByDescending(user => user.LastLogin) : query.OrderBy(user => user.LastLogin),
        "twofactorenabled" => descending ? query.OrderByDescending(user => user.TwoFactorEnabled) : query.OrderBy(user => user.TwoFactorEnabled),
        _ => descending ? query.OrderByDescending(user => user.Name) : query.OrderBy(user => user.Name)
    };

    var total = await filteredQuery.CountAsync();
    var stats = await db.Users
        .AsNoTracking()
        .GroupBy(_ => 1)
        .Select(group => new UserStatsDto(
            group.Count(),
            group.Count(user => user.Status == "Active"),
            group.Count(user => user.Role!.Name == "Driver"),
            group.Count(user => user.Role!.Name == "Admin")))
        .FirstOrDefaultAsync() ?? new UserStatsDto(0, 0, 0, 0);

    var userItems = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(user => new UserListItem(
            user.Id,
            user.Name,
            user.EmployeeId,
            user.NrcNumber,
            user.Email,
            user.Role!.Name,
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
            user.JoinDate,
            filteredQuery.Count(item => item.Id <= user.Id)))
        .ToListAsync();

    return Results.Ok(new UserPageDto(userItems.Select(user => ToUserListDto(user, httpContext)).ToList(), total, page, pageSize, stats));
});

users.MapGet("/{id:int}", async (int id, FleetDbContext db, HttpContext httpContext) =>
{
    var user = await db.Users
        .AsNoTracking()
        .Where(item => item.Id == id)
        .Select(item => new UserListItem(
            item.Id,
            item.Name,
            item.EmployeeId,
            item.NrcNumber,
            item.Email,
            item.Role!.Name,
            item.Status,
            item.Phone,
            item.Avatar,
            item.NrcFront,
            item.NrcBack,
            item.Department,
            item.Title,
            item.Location,
            item.Manager,
            item.LicenseNumber,
            item.LicenseClass,
            item.LicenseExpiry,
            item.EmergencyContactName,
            item.EmergencyContactRelation,
            item.EmergencyContactPhone,
            item.Address,
            item.LastLogin,
            item.TwoFactorEnabled,
            item.Notes,
            item.JoinDate,
            null))
        .FirstOrDefaultAsync();

    return user is null ? Results.NotFound() : Results.Ok(ToUserListDto(user, httpContext));
});

users.MapGet("/{id:int}/images/{kind}", async (int id, string kind, FleetDbContext db, HttpContext httpContext) =>
{
    if (!IsSupportedImageKind(kind))
    {
        return Results.BadRequest(new { message = "Image kind must be avatar, nrc-front, or nrc-back." });
    }

    IQueryable<string?> imageQuery = kind switch
    {
        "avatar" => db.Users.AsNoTracking().Where(user => user.Id == id).Select(user => user.Avatar),
        "nrc-front" => db.Users.AsNoTracking().Where(user => user.Id == id).Select(user => user.NrcFront),
        "nrc-back" => db.Users.AsNoTracking().Where(user => user.Id == id).Select(user => user.NrcBack),
        _ => throw new InvalidOperationException("Unsupported image kind.")
    };

    var image = await imageQuery.FirstOrDefaultAsync();
    if (image is null) return Results.NotFound();

    if (IsUploadPath(image))
    {
        return Results.Redirect(ToAbsoluteUrl(httpContext, image));
    }

    if (Uri.TryCreate(image, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
    {
        return Results.Redirect(image);
    }

    var parsedImage = ParseDataUri(image);
    if (parsedImage is null) return Results.NotFound();

    httpContext.Response.Headers.CacheControl = "public,max-age=3600";
    return Results.File(parsedImage.Value.Bytes, parsedImage.Value.ContentType);
});

users.MapPost("/", async (UserRequest request, FleetDbContext db, HttpContext httpContext, IWebHostEnvironment environment) =>
{
    var validationError = ValidateUserRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var role = await FindRoleByNameAsync(request.Role, db);
    if (role is null) return Results.BadRequest(new { message = "Selected role does not exist." });

    var userCodeOptionError = await ValidateUserCodeSelectionsAsync(request, db);
    if (userCodeOptionError is not null) return Results.BadRequest(new { message = userCodeOptionError });

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

    user.Avatar = await SaveImageFieldAsync(environment, user.Id, "avatar", request.Avatar);
    user.NrcFront = await SaveImageFieldAsync(environment, user.Id, "nrc-front", request.NrcFront);
    user.NrcBack = await SaveImageFieldAsync(environment, user.Id, "nrc-back", request.NrcBack);
    await db.SaveChangesAsync();

    user.Role = role;

    return Results.Created($"/api/users/{user.Id}", ToUserEntityDto(user, httpContext));
});

users.MapPut("/{id:int}", async (int id, UserRequest request, FleetDbContext db, HttpContext httpContext, IWebHostEnvironment environment) =>
{
    var validationError = ValidateUserRequest(request);
    if (validationError is not null) return Results.BadRequest(new { message = validationError });

    var user = await db.Users
        .Include(item => item.Role)
        .FirstOrDefaultAsync(item => item.Id == id);
    if (user is null) return Results.NotFound();

    var role = await FindRoleByNameAsync(request.Role, db);
    if (role is null) return Results.BadRequest(new { message = "Selected role does not exist." });

    var userCodeOptionError = await ValidateUserCodeSelectionsAsync(request, db);
    if (userCodeOptionError is not null) return Results.BadRequest(new { message = userCodeOptionError });

    var duplicateError = await ValidateUniqueUserFieldsAsync(request, id, db);
    if (duplicateError is not null) return Results.Conflict(new { message = duplicateError });

    user.Name = request.Name.Trim();
    user.EmployeeId = request.EmployeeId.Trim();
    user.NrcNumber = request.NrcNumber.Trim();
    user.Email = request.Email.Trim();
    user.Phone = request.Phone.Trim();
    user.Status = request.Status.Trim();
    user.Avatar = await SaveOrPreserveImageFieldAsync(environment, user.Avatar, request.Avatar, id, "avatar");
    user.NrcFront = await SaveOrPreserveImageFieldAsync(environment, user.NrcFront, request.NrcFront, id, "nrc-front");
    user.NrcBack = await SaveOrPreserveImageFieldAsync(environment, user.NrcBack, request.NrcBack, id, "nrc-back");
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

    return Results.Ok(ToUserEntityDto(user, httpContext));
});

users.MapPatch("/{id:int}/status", async (int id, UserStatusRequest request, FleetDbContext db, HttpContext httpContext) =>
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

    return Results.Ok(ToUserEntityDto(user, httpContext));
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

static string? ValidateUserCodeOptionRequest(UserCodeOptionRequest request)
{
    var normalizedType = NormalizeUserCodeOptionType(request.Type);
    if (normalizedType is null) return "Type must be Department or Location.";
    if (string.IsNullOrWhiteSpace(request.Name)) return $"{normalizedType} name is required.";
    if (string.IsNullOrWhiteSpace(request.Status)) return $"{normalizedType} status is required.";

    var status = request.Status.Trim();
    return status is "Active" or "Disabled"
        ? null
        : $"{normalizedType} status must be Active or Disabled.";
}

static string? NormalizeUserCodeOptionType(string? value)
{
    var normalized = value?.Trim();
    return normalized switch
    {
        "Department" => "Department",
        "Location" => "Location",
        "Location / Depot" => "Location",
        _ => null
    };
}

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

static UserDto ToUserListDto(UserListItem user, HttpContext httpContext) => new(
    user.Id,
    user.DisplayOrder,
    user.Name,
    user.EmployeeId,
    user.NrcNumber,
    user.Email,
    user.Role,
    user.Status,
    user.Phone,
    ResolveStoredImageUrl(httpContext, user.Avatar, user.Id, "avatar"),
    ResolveStoredImageUrl(httpContext, user.NrcFront, user.Id, "nrc-front"),
    ResolveStoredImageUrl(httpContext, user.NrcBack, user.Id, "nrc-back"),
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

static UserDto ToUserEntityDto(User user, HttpContext httpContext) => new(
    user.Id,
    null,
    user.Name,
    user.EmployeeId,
    user.NrcNumber,
    user.Email,
    user.Role?.Name ?? "",
    user.Status,
    user.Phone,
    ResolveStoredImageUrl(httpContext, user.Avatar, user.Id, "avatar"),
    ResolveStoredImageUrl(httpContext, user.NrcFront, user.Id, "nrc-front"),
    ResolveStoredImageUrl(httpContext, user.NrcBack, user.Id, "nrc-back"),
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

static async Task<string?> ValidateUserCodeSelectionsAsync(UserRequest request, FleetDbContext db)
{
    var department = request.Department.Trim();
    var location = request.Location.Trim();

    var availableDepartments = await db.DepartmentCodeOptions
        .AsNoTracking()
        .Where(option => option.Status == "Active" && option.Name == department)
        .Select(option => option.Name)
        .ToListAsync();

    var availableLocations = await db.LocationCodeOptions
        .AsNoTracking()
        .Where(option => option.Status == "Active" && option.Name == location)
        .Select(option => option.Name)
        .ToListAsync();

    var hasDepartment = availableDepartments.Count > 0;
    if (!hasDepartment) return "Selected department does not exist in code setup.";

    var hasLocation = availableLocations.Count > 0;
    if (!hasLocation) return "Selected location does not exist in code setup.";

    return null;
}

static async Task<string?> ValidateUniqueUserFieldsAsync(UserRequest request, int? userId, FleetDbContext db)
{
    var employeeId = request.EmployeeId.Trim();
    var nrcNumber = request.NrcNumber.Trim();
    var email = request.Email.Trim();

    var duplicate = await db.Users
        .AsNoTracking()
        .Where(user =>
        (!userId.HasValue || user.Id != userId.Value) &&
        (user.EmployeeId == employeeId || user.NrcNumber == nrcNumber || user.Email == email))
        .Select(user => new { user.EmployeeId, user.NrcNumber, user.Email })
        .FirstOrDefaultAsync();

    if (duplicate is null) return null;

    if (duplicate.EmployeeId == employeeId)
    {
        return "Employee ID already exists.";
    }

    if (duplicate.NrcNumber == nrcNumber)
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
    if (!IsValidEmail(request.Email)) return "Enter a valid email address.";
    if (string.IsNullOrWhiteSpace(request.Phone)) return "Phone number is required.";
    if (!IsValidPhone(request.Phone)) return "Enter a valid phone number.";
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
    if (!IsValidPhone(request.EmergencyContactPhone)) return "Enter a valid emergency contact phone number.";
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

static bool IsValidEmail(string value)
{
    try
    {
        var trimmed = value.Trim();
        var address = new MailAddress(trimmed);
        return address.Address == trimmed && address.Host.Contains('.');
    }
    catch (FormatException)
    {
        return false;
    }
}

static bool IsValidPhone(string value)
{
    var trimmed = value.Trim();
    var digitCount = trimmed.Count(char.IsDigit);
    return digitCount is >= 7 and <= 15 && Regex.IsMatch(trimmed, @"^\+?[\d\s().-]{7,24}$");
}

static async Task<string?> SaveOrPreserveImageFieldAsync(
    IWebHostEnvironment environment,
    string? currentValue,
    string? requestValue,
    int userId,
    string kind)
{
    var normalized = NormalizeOptional(requestValue);
    if (IsApiImageUrl(normalized, userId, kind) || normalized == currentValue)
    {
        return currentValue;
    }

    return await SaveImageFieldAsync(environment, userId, kind, normalized);
}

static async Task<string?> SaveImageFieldAsync(IWebHostEnvironment environment, int userId, string kind, string? value)
{
    var normalized = NormalizeOptional(value);
    if (normalized is null) return null;

    if (IsUploadPath(normalized) || IsHttpUrl(normalized))
    {
        return normalized;
    }

    var parsedImage = ParseDataUri(normalized);
    if (parsedImage is null)
    {
        return normalized;
    }

    var extension = ExtensionForContentType(parsedImage.Value.ContentType);
    var uploadDirectory = Path.Combine(UploadRoot(environment), userId.ToString());
    Directory.CreateDirectory(uploadDirectory);

    DeleteExistingImageFiles(uploadDirectory, kind);

    var fileName = $"{kind}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
    var filePath = Path.Combine(uploadDirectory, fileName);
    await File.WriteAllBytesAsync(filePath, parsedImage.Value.Bytes);

    return $"/uploads/users/{userId}/{fileName}";
}

static bool IsApiImageUrl(string? value, int userId, string kind)
{
    return value?.Contains($"/api/users/{userId}/images/{kind}", StringComparison.OrdinalIgnoreCase) == true;
}

static bool IsSupportedImageKind(string kind)
{
    return kind is "avatar" or "nrc-front" or "nrc-back";
}

static bool IsUploadPath(string value)
{
    return value.StartsWith("/uploads/users/", StringComparison.OrdinalIgnoreCase);
}

static bool IsHttpUrl(string value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

static string? ResolveStoredImageUrl(HttpContext httpContext, string? value, int userId, string kind)
{
    var normalized = NormalizeOptional(value);
    if (normalized is null) return null;
    if (IsUploadPath(normalized)) return ToAbsoluteUrl(httpContext, normalized);
    if (IsHttpUrl(normalized)) return normalized;
    return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/users/{userId}/images/{kind}";
}

static string ToAbsoluteUrl(HttpContext httpContext, string relativePath)
{
    return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{relativePath}";
}

static string UploadRoot(IWebHostEnvironment environment)
{
    var webRoot = environment.WebRootPath;
    if (string.IsNullOrWhiteSpace(webRoot))
    {
        webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    return Path.Combine(webRoot, "uploads", "users");
}

static string ExtensionForContentType(string contentType)
{
    return contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/jpg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        _ => ".bin"
    };
}

static void DeleteExistingImageFiles(string uploadDirectory, string kind)
{
    foreach (var filePath in Directory.EnumerateFiles(uploadDirectory, $"{kind}.*"))
    {
        File.Delete(filePath);
    }
}

static ParsedImage? ParseDataUri(string value)
{
    if (!value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

    var separatorIndex = value.IndexOf(',');
    if (separatorIndex < 0) return null;

    var metadata = value[5..separatorIndex];
    if (!metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase)) return null;

    var contentType = metadata[..^7];
    if (string.IsNullOrWhiteSpace(contentType)) return null;

    try
    {
        return new ParsedImage(Convert.FromBase64String(value[(separatorIndex + 1)..]), contentType);
    }
    catch (FormatException)
    {
        return null;
    }
}

static async Task MigrateUserImagesToFileStorageAsync(FleetDbContext db, IWebHostEnvironment environment)
{
    var users = await db.Users
        .AsNoTracking()
        .Where(user =>
            user.Avatar != null && user.Avatar.StartsWith("data:") ||
            user.NrcFront != null && user.NrcFront.StartsWith("data:") ||
            user.NrcBack != null && user.NrcBack.StartsWith("data:"))
        .Select(user => new ImageMigrationItem(user.Id, user.Avatar, user.NrcFront, user.NrcBack))
        .ToListAsync();

    foreach (var user in users)
    {
        var avatar = await SaveImageFieldAsync(environment, user.Id, "avatar", user.Avatar);
        var nrcFront = await SaveImageFieldAsync(environment, user.Id, "nrc-front", user.NrcFront);
        var nrcBack = await SaveImageFieldAsync(environment, user.Id, "nrc-back", user.NrcBack);

        await db.Users
            .Where(item => item.Id == user.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Avatar, avatar)
                .SetProperty(item => item.NrcFront, nrcFront)
                .SetProperty(item => item.NrcBack, nrcBack));
    }
}

static async Task EnsureUserCodeOptionSchemaAsync(FleetDbContext db)
{
    var schemaSql = """
IF OBJECT_ID('UserCodeOptions', 'U') IS NULL
BEGIN
    CREATE TABLE [UserCodeOptions]
    (
        [Id] int NOT NULL IDENTITY(1,1),
        [Type] nvarchar(30) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(300) NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT DF_UserCodeOptions_Status DEFAULT 'Active',
        [CreatedAt] datetimeoffset NOT NULL CONSTRAINT DF_UserCodeOptions_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_UserCodeOptions] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_UserCodeOptions_Type_Name] ON [UserCodeOptions] ([Type], [Name]);
END

IF OBJECT_ID('UserCodeOptions', 'U') IS NOT NULL
BEGIN
    UPDATE [UserCodeOptions]
    SET [Type] = 'Location'
    WHERE [Type] = 'Location / Depot';

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_UserCodeOptions_Type'
          AND parent_object_id = OBJECT_ID('UserCodeOptions')
    )
    BEGIN
        ALTER TABLE [UserCodeOptions]
        ADD CONSTRAINT [CK_UserCodeOptions_Type] CHECK ([Type] IN ('Department', 'Location'));
    END
END
""";

    await db.Database.ExecuteSqlRawAsync(schemaSql);
}

static async Task EnsureSeparatedUserCodeOptionSchemaAsync(FleetDbContext db)
{
    var schemaSql = """
IF OBJECT_ID('DepartmentCodeOptions', 'U') IS NULL
BEGIN
    CREATE TABLE [DepartmentCodeOptions]
    (
        [Id] int NOT NULL IDENTITY(1,1),
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(300) NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT DF_DepartmentCodeOptions_Status DEFAULT 'Active',
        [CreatedAt] datetimeoffset NOT NULL CONSTRAINT DF_DepartmentCodeOptions_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_DepartmentCodeOptions] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_DepartmentCodeOptions_Name] ON [DepartmentCodeOptions] ([Name]);
END

IF OBJECT_ID('LocationCodeOptions', 'U') IS NULL
BEGIN
    CREATE TABLE [LocationCodeOptions]
    (
        [Id] int NOT NULL IDENTITY(1,1),
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(300) NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT DF_LocationCodeOptions_Status DEFAULT 'Active',
        [CreatedAt] datetimeoffset NOT NULL CONSTRAINT DF_LocationCodeOptions_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_LocationCodeOptions] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_LocationCodeOptions_Name] ON [LocationCodeOptions] ([Name]);
END

IF OBJECT_ID('UserCodeOptions', 'U') IS NOT NULL
BEGIN
    INSERT INTO [DepartmentCodeOptions] ([Name], [Description], [Status], [CreatedAt], [UpdatedAt])
    SELECT legacy.[Name], legacy.[Description], legacy.[Status], legacy.[CreatedAt], legacy.[UpdatedAt]
    FROM [UserCodeOptions] legacy
    WHERE legacy.[Type] = 'Department'
      AND NOT EXISTS (
          SELECT 1 FROM [DepartmentCodeOptions] d WHERE d.[Name] = legacy.[Name]
      );

    INSERT INTO [LocationCodeOptions] ([Name], [Description], [Status], [CreatedAt], [UpdatedAt])
    SELECT legacy.[Name], legacy.[Description], legacy.[Status], legacy.[CreatedAt], legacy.[UpdatedAt]
    FROM [UserCodeOptions] legacy
    WHERE legacy.[Type] = 'Location'
      AND NOT EXISTS (
          SELECT 1 FROM [LocationCodeOptions] l WHERE l.[Name] = legacy.[Name]
      );
END
""";

    await db.Database.ExecuteSqlRawAsync(schemaSql);
}

static async Task EnsureUserSchemaAsync(FleetDbContext db)
{
    var schemaSql = """
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
""";

    await db.Database.ExecuteSqlRawAsync(schemaSql);

    var backfillSql = """
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
    [LastLogin] = COALESCE([LastLogin], SYSDATETIMEOFFSET())
WHERE
    [EmployeeId] = ''
    OR [NrcNumber] = ''
    OR [Department] = ''
    OR [Title] = ''
    OR [Location] = ''
    OR [Manager] = ''
    OR [EmergencyContactName] = ''
    OR [EmergencyContactRelation] = ''
    OR [EmergencyContactPhone] = ''
    OR [Address] = ''
    OR [Avatar] IS NULL
    OR [NrcFront] IS NULL
    OR [NrcBack] IS NULL
    OR [LastLogin] IS NULL;
""";

    await db.Database.ExecuteSqlRawAsync(backfillSql);
}

sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

sealed record UserPageDto(IReadOnlyList<UserDto> Items, int Total, int Page, int PageSize, UserStatsDto Stats);

sealed record UserCodeOptionDto(
    int Id,
    string Type,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int? DisplayOrder = null);

sealed record UserStatsDto(int Total, int Active, int Drivers, int Admins);

sealed record ImageMigrationItem(int Id, string? Avatar, string? NrcFront, string? NrcBack);

sealed record RoleMemberListItem(
    int Id,
    string Name,
    string Email,
    string Phone,
    string Status,
    DateOnly JoinDate,
    string? Avatar);

sealed record UserListItem(
    int Id,
    string Name,
    string EmployeeId,
    string NrcNumber,
    string Email,
    string Role,
    string Status,
    string Phone,
    string? Avatar,
    string? NrcFront,
    string? NrcBack,
    string Department,
    string Title,
    string Location,
    string Manager,
    string? LicenseNumber,
    string? LicenseClass,
    DateOnly? LicenseExpiry,
    string EmergencyContactName,
    string EmergencyContactRelation,
    string EmergencyContactPhone,
    string Address,
    DateTimeOffset? LastLogin,
    bool TwoFactorEnabled,
    string? Notes,
    DateOnly JoinDate,
    int? DisplayOrder);

readonly record struct ParsedImage(byte[] Bytes, string ContentType);
