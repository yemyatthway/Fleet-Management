using FleetManagement.Api.Assets;
using FleetManagement.Api.Data;
using FleetManagement.Api.Dtos;
using FleetManagement.Api.Email;
using FleetManagement.Api.Models;
using FleetManagement.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api.Endpoints;

public static class AuthEndpoints
{
  public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapPost("/api/auth/login", async (
      LoginRequest request,
      HttpRequest httpRequest,
      FleetDbContext db,
      IConfiguration configuration,
      OtpChallengeStore otpStore,
      IEmailSender emailSender) =>
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

      if (user.TwoFactorEnabled)
      {
        var challenge = otpStore.Create(user.Id, request.RememberMe);
        try
        {
          await emailSender.SendAsync(
            user.Email,
            "FleetManager login verification code",
            $"Your FleetManager verification code is {challenge.Code}. It expires in 10 minutes.");
        }
        catch
        {
          otpStore.Remove(challenge.ChallengeId);
          return Results.Json(
            new ApiError("Could not send verification email. Check Gmail SMTP app password settings, then try again."),
            statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new LoginResponseDto(
          null,
          [],
          null,
          null,
          true,
          challenge.ChallengeId,
          $"A verification code was sent to {MaskEmail(user.Email)}."));
      }

      return Results.Ok(await BuildLoginResponseAsync(user, httpRequest, db, configuration, request.RememberMe));
    });

    app.MapPost("/api/auth/verify-otp", async (
      VerifyOtpRequest request,
      HttpRequest httpRequest,
      FleetDbContext db,
      IConfiguration configuration,
      OtpChallengeStore otpStore) =>
    {
      if (string.IsNullOrWhiteSpace(request.ChallengeId) || string.IsNullOrWhiteSpace(request.Code))
      {
        return Results.BadRequest(new ApiError("Verification code is required."));
      }

      if (!otpStore.TryConsume(request.ChallengeId, request.Code, out var challenge))
      {
        return Results.BadRequest(new ApiError("Invalid or expired verification code."));
      }

      var user = await db.Users
        .Include(item => item.Role)
        .FirstOrDefaultAsync(item =>
          item.Id == challenge.UserId &&
          item.IsDeleted == 0 &&
          item.Role != null &&
          item.Role.IsDeleted == 0);

      if (user is null || !string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
      {
        return Results.BadRequest(new ApiError("This user account is not active."));
      }

      return Results.Ok(await BuildLoginResponseAsync(user, httpRequest, db, configuration, challenge.RememberMe));
    });

    return app;
  }

  private static async Task<LoginResponseDto> BuildLoginResponseAsync(
    User user,
    HttpRequest httpRequest,
    FleetDbContext db,
    IConfiguration configuration,
    bool rememberMe)
  {
    user.LastLogin = DateTime.UtcNow.ToString("o");
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var permissions = await PermissionChecks.GetPermissionsForRoleAsync(db, user.RoleId);
    var tokenResult = JwtTokenService.CreateToken(
      configuration,
      new JwtUserContext(user.Id, user.Name, user.Email, user.RoleId, user.Role!.Name),
      rememberMe);

    return new LoginResponseDto(
        new AuthUserDto(
          user.Id,
          user.Name,
          user.Email,
          user.RoleId,
          user.Role!.Name,
          user.Status,
          PublicAssetUrls.ToPublicAssetUrl(httpRequest, user.Avatar)),
        permissions,
        tokenResult.Token,
        tokenResult.ExpiresAt);
  }

  private static string MaskEmail(string email)
  {
    var parts = email.Split('@', 2);
    if (parts.Length != 2 || parts[0].Length <= 2) return email;
    return $"{parts[0][..2]}***@{parts[1]}";
  }
}
