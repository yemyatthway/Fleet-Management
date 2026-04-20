using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueClient", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

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
