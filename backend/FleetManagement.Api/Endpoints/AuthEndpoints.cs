using FleetManagement.Api.Assets;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class AuthEndpoints
{
  public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
  {
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

      var permissions = await PermissionChecks.GetPermissionsForRoleAsync(db, user.RoleId);
      return Results.Ok(new LoginResponseDto(
        new AuthUserDto(
          user.Id,
          user.Name,
          user.Email,
          user.RoleId,
          user.Role!.Name,
          user.Status,
          PublicAssetUrls.ToPublicAssetUrl(httpRequest, user.Avatar)),
        permissions));
    });

    return app;
  }
}
