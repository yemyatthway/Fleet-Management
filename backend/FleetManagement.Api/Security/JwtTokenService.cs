using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FleetManagement.Api.Security;

public record JwtUserContext(string UserId, string Name, string Email, string RoleId, string Role);

public static class JwtTokenService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static (string Token, DateTime ExpiresAt) CreateToken(
    IConfiguration configuration,
    JwtUserContext user,
    bool rememberMe)
  {
    var issuedAt = DateTimeOffset.UtcNow;
    var expiresAt = issuedAt.Add(rememberMe ? TimeSpan.FromDays(14) : TimeSpan.FromHours(8));
    var header = new Dictionary<string, object>
    {
      ["alg"] = "HS256",
      ["typ"] = "JWT"
    };
    var payload = new Dictionary<string, object>
    {
      ["sub"] = user.UserId,
      ["name"] = user.Name,
      ["email"] = user.Email,
      ["roleId"] = user.RoleId,
      ["role"] = user.Role,
      ["iat"] = issuedAt.ToUnixTimeSeconds(),
      ["exp"] = expiresAt.ToUnixTimeSeconds(),
      ["jti"] = Guid.NewGuid().ToString("N")
    };

    var unsignedToken = $"{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header, JsonOptions))}.{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions))}";
    var signature = Sign(unsignedToken, GetSecret(configuration));
    return ($"{unsignedToken}.{signature}", expiresAt.UtcDateTime);
  }

  public static bool TryValidateToken(IConfiguration configuration, string? token, out JwtUserContext user)
  {
    user = new JwtUserContext(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    if (string.IsNullOrWhiteSpace(token)) return false;

    var parts = token.Split('.');
    if (parts.Length != 3) return false;

    var unsignedToken = $"{parts[0]}.{parts[1]}";
    var expectedSignature = Sign(unsignedToken, GetSecret(configuration));
    if (!FixedTimeEquals(parts[2], expectedSignature)) return false;

    Dictionary<string, JsonElement>? payload;
    try
    {
      payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64UrlDecode(parts[1]), JsonOptions);
    }
    catch
    {
      return false;
    }

    if (payload is null) return false;
    if (!payload.TryGetValue("exp", out var expClaim) || !expClaim.TryGetInt64(out var exp)) return false;
    if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp) return false;

    var userId = GetStringClaim(payload, "sub");
    var name = GetStringClaim(payload, "name");
    var email = GetStringClaim(payload, "email");
    var roleId = GetStringClaim(payload, "roleId");
    var role = GetStringClaim(payload, "role");
    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleId)) return false;

    user = new JwtUserContext(userId, name, email, roleId, role);
    return true;
  }

  public static string? GetBearerToken(HttpRequest request)
  {
    var authorization = request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(authorization)) return null;
    const string bearerPrefix = "Bearer ";
    return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
      ? authorization[bearerPrefix.Length..].Trim()
      : null;
  }

  private static string GetSecret(IConfiguration configuration) =>
    configuration["Jwt:Secret"] ??
    "FleetManagementDevelopmentJwtSecretChangeThisBeforeProduction2026";

  private static string Sign(string value, string secret)
  {
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
  }

  private static bool FixedTimeEquals(string left, string right)
  {
    var leftBytes = Encoding.UTF8.GetBytes(left);
    var rightBytes = Encoding.UTF8.GetBytes(right);
    return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
  }

  private static string GetStringClaim(Dictionary<string, JsonElement> payload, string key) =>
    payload.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
      ? value.GetString() ?? string.Empty
      : string.Empty;

  private static string Base64UrlEncode(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

  private static byte[] Base64UrlDecode(string value)
  {
    var base64 = value.Replace('-', '+').Replace('_', '/');
    base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    return Convert.FromBase64String(base64);
  }
}
